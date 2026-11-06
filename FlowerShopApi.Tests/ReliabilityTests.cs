
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using NUnit.Framework;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace FlowerShopApi.Tests;

[TestFixture]
public class ReliabilityTests
{
    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage,
            CancellationToken, Task<HttpResponseMessage>> _response;

        public int CallCount;

        public FakeHttpHandler(
            Func<HttpRequestMessage, CancellationToken,
                Task<HttpResponseMessage>> response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref CallCount);
            return _response(request, cancellationToken);
        }
    }

    private static ServiceProvider CreateProvider(
        FakeHttpHandler handler,
        Action<HttpStandardResilienceOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddHttpClient("ReliabilityTest", client =>
        {
            client.BaseAddress = new Uri("http://localhost/");
        })
        .ConfigurePrimaryHttpMessageHandler(() => handler)
        .AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout =
                TimeSpan.FromSeconds(10);

            options.AttemptTimeout.Timeout =
                TimeSpan.FromSeconds(1);

            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay =
                TimeSpan.FromMilliseconds(10);
            options.Retry.BackoffType =
                DelayBackoffType.Constant;
            options.Retry.UseJitter = false;
            options.Retry.DisableForUnsafeHttpMethods();

            options.CircuitBreaker.MinimumThroughput = 100;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.SamplingDuration =
                TimeSpan.FromSeconds(10);
            options.CircuitBreaker.BreakDuration =
                TimeSpan.FromSeconds(2);

            configure?.Invoke(options);
        });

        return services.BuildServiceProvider();
    }

    [Test]
    public async Task Retry_ShouldRepeatFailedRequests()
    {
        var handler = new FakeHttpHandler((_, _) =>
            Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.ServiceUnavailable)));

        using var provider = CreateProvider(handler);
        var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("ReliabilityTest");

        using var response = await client.GetAsync("test");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode,
                Is.EqualTo(HttpStatusCode.ServiceUnavailable));

            Assert.That(handler.CallCount, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Retry_ShouldNotRepeatPostRequests()
    {
        var handler = new FakeHttpHandler((_, _) =>
            Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.ServiceUnavailable)));

        using var provider = CreateProvider(handler);
        var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("ReliabilityTest");

        using var response = await client.PostAsync(
            "test", new StringContent("{}"));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode,
                Is.EqualTo(HttpStatusCode.ServiceUnavailable));

            Assert.That(handler.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Timeout_ShouldCancelSlowRequest()
    {
        var handler = new FakeHttpHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(
                TimeSpan.FromSeconds(3), cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var provider = CreateProvider(handler, options =>
        {
            options.AttemptTimeout.Timeout =
                TimeSpan.FromMilliseconds(150);

            options.Retry.MaxRetryAttempts = 1;
        });

        var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("ReliabilityTest");

        Assert.ThrowsAsync<TimeoutRejectedException>(
            async () => await client.GetAsync("test"));

        Assert.That(handler.CallCount, Is.EqualTo(2));
    }

    [Test]
    public async Task CircuitBreaker_ShouldOpenAndRecover()
    {
        var healthy = false;

        var handler = new FakeHttpHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(
                healthy
                    ? HttpStatusCode.OK
                    : HttpStatusCode.ServiceUnavailable)));

        using var provider = CreateProvider(handler, options =>
        {
            options.Retry.MaxRetryAttempts = 1;
            options.Retry.ShouldHandle = _ => ValueTask.FromResult(false);

            options.CircuitBreaker.MinimumThroughput = 2;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.SamplingDuration =
                TimeSpan.FromSeconds(10);
            options.CircuitBreaker.BreakDuration =
                TimeSpan.FromSeconds(2);
        });

        var client = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("ReliabilityTest");

        using (var first = await client.GetAsync("test"))
        using (var second = await client.GetAsync("test"))
        {
            Assert.That(first.StatusCode,
                Is.EqualTo(HttpStatusCode.ServiceUnavailable));

            Assert.That(second.StatusCode,
                Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        }

        Assert.ThrowsAsync<BrokenCircuitException>(
            async () => await client.GetAsync("test"));

        Assert.That(handler.CallCount, Is.EqualTo(2),
            "Open circuit must block outgoing requests");

        healthy = true;

        await Task.Delay(TimeSpan.FromMilliseconds(2300));

        using var recovered = await client.GetAsync("test");

        Assert.Multiple(() =>
        {
            Assert.That(recovered.StatusCode,
                Is.EqualTo(HttpStatusCode.OK));

            Assert.That(handler.CallCount, Is.EqualTo(3));
        });
    }
}
