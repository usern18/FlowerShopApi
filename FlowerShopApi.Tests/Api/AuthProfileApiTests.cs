using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;

namespace FlowerShopApi.Tests.Api;

[TestFixture]
public class AuthProfileApiTests
{
    private const string Email = "api-user@example.com";
    private const string Password = "Password123!";
    private const string Phone = "0991234567";

    private static object Registration() => new
    {
        firstName = "Test",
        lastName = "User",
        email = Email,
        phone = Phone,
        password = Password
    };

    private static async Task<(FlowerShopApiTestFactory Factory, HttpClient Client)> StartAsync()
    {
        var factory = new FlowerShopApiTestFactory();
        var client = factory.CreateClient();
        await factory.SeedCustomerRoleAsync();
        return (factory, client);
    }

    private static async Task RegisterAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", Registration());
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = Email,
            password = Password
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        Assert.That(token, Is.Not.Null.And.Not.Empty);
        return token!;
    }

    [Test]
    public async Task Register_ShouldReturn200AndSuccessMessage_WhenRequestIsValid()
    {
        var (factory, client) = await StartAsync();
        using (factory)
        using (client)
        {
            using var response = await client.PostAsJsonAsync("/api/auth/register", Registration());
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.That(json.RootElement.GetProperty("message").GetString(),
                Is.EqualTo("Реєстрація успішна"));
        }
    }

    [Test]
    public async Task Login_ShouldReturn200AndTokens_WhenCredentialsAreValid()
    {
        var (factory, client) = await StartAsync();
        using (factory)
        using (client)
        {
            await RegisterAsync(client);
            using var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = Email,
                password = Password
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Multiple(() =>
            {
                Assert.That(json.RootElement.GetProperty("accessToken").GetString(), Is.Not.Empty);
                Assert.That(json.RootElement.GetProperty("refreshToken").GetString(), Is.Not.Empty);
                Assert.That(json.RootElement.GetProperty("email").GetString(), Is.EqualTo(Email));
            });
        }
    }

    [Test]
    public async Task Me_ShouldReturn200AndCurrentUser_WhenJwtIsValid()
    {
        var (factory, client) = await StartAsync();
        using (factory)
        using (client)
        {
            await RegisterAsync(client);
            var token = await LoginAndGetTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.GetAsync("/api/auth/me");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.That(json.RootElement.GetProperty("email").GetString(), Is.EqualTo(Email));
        }
    }

    [Test]
    public async Task UpdatePersonalInfo_ShouldReturn200AndTrimmedFields_WhenJwtIsValid()
    {
        var (factory, client) = await StartAsync();
        using (factory)
        using (client)
        {
            await RegisterAsync(client);
            var token = await LoginAndGetTokenAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.PutAsJsonAsync("/api/profile/personal-info", new
            {
                firstName = "  New  ",
                lastName = "  Surname  ",
                middleName = " Middle ",
                phone = Phone
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Multiple(() =>
            {
                Assert.That(json.RootElement.GetProperty("firstName").GetString(), Is.EqualTo("New"));
                Assert.That(json.RootElement.GetProperty("lastName").GetString(), Is.EqualTo("Surname"));
                Assert.That(json.RootElement.GetProperty("middleName").GetString(), Is.EqualTo("Middle"));
            });
        }
    }
}
