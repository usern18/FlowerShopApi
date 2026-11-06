
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlowerShopApi.DTOs.Auth;
using NUnit.Framework;

namespace FlowerShopApi.IntegrationTests;

[TestFixture]
public class ApiSecurityTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        _factory.ResetDatabase();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> LoginAsync(
        string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto
            {
                Email = email,
                Password = password
            });

        response.EnsureSuccessStatusCode();

        var body = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        return body.TryGetProperty(
            "accessToken", out var token)
            ? token.GetString()!
            : body.GetProperty("AccessToken").GetString()!;
    }

    [Test]
    public async Task GetMyOrders_WithoutToken_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/orders/my");

        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetAllOrders_AsCustomer_ShouldReturn403()
    {
        var token = await LoginAsync(
            "test@example.com", "Password123!");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/orders");

        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task CreateOrder_WithNegativeQuantity_ShouldReturn400()
    {
        var token = await LoginAsync(
            "test@example.com", "Password123!");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var invalidOrder = new
        {
            deliveryAddress = "Київ, Хрещатик 10",
            recipientName = "Тест",
            recipientPhone = "+380501111111",
            items = new[]
            {
                new
                {
                    productId = 1,
                    quantity = -5
                }
            }
        };

        var response = await _client.PostAsJsonAsync(
            "/api/orders", invalidOrder);

        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetMyOrders_WithInvalidToken_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "invalid.jwt.token"
            );

        var response = await _client.GetAsync("/api/orders/my");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized)
        );
    }
}
