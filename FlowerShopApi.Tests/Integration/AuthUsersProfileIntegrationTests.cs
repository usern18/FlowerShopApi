using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlowerShopApi.Data;
using FlowerShopApi.Tests.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace FlowerShopApi.Tests.Integration;

[TestFixture]
public sealed class AuthUsersProfileIntegrationTests
{
    private const string Email = "integration-user@example.com";
    private const string Password = "Password123!";
    private const string Phone = "0991234567";

    private static async Task<(FlowerShopApiTestFactory Factory, HttpClient Client)> StartAsync()
    {
        var factory = new FlowerShopApiTestFactory();
        var client = factory.CreateClient();
        await factory.SeedCustomerRoleAsync();
        return (factory, client);
    }

    private static async Task RegisterAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email = Email,
            phone = Phone,
            password = Password
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = Email,
            password = Password
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accessToken = json.RootElement.GetProperty("accessToken").GetString();
        Assert.That(accessToken, Is.Not.Null.And.Not.Empty);
        return accessToken!;
    }

    [Test]
    public async Task Register_ShouldPersistCustomerWithHashedPassword_WhenRequestIsValid()
    {
        var (factory, client) = await StartAsync();
        using (factory)
        using (client)
        {
            await RegisterAsync(client);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking().Include(x => x.Role)
                .SingleAsync(x => x.Email == Email);

            Assert.Multiple(() =>
            {
                Assert.That(user.FirstName, Is.EqualTo("Test"));
                Assert.That(user.Role?.RoleName, Is.EqualTo("customer"));
                Assert.That(user.PasswordHash, Is.Not.EqualTo(Password));
                Assert.That(BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash), Is.True);
            });
        }
    }

    [Test]
    public async Task Login_ShouldPersistRefreshToken_WhenCredentialsAreValid()
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
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                await response.Content.ReadAsStringAsync());
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var accessToken = json.RootElement.GetProperty("accessToken").GetString();
            var refreshToken = json.RootElement.GetProperty("refreshToken").GetString();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking().SingleAsync(x => x.Email == Email);

            Assert.Multiple(() =>
            {
                Assert.That(accessToken, Is.Not.Null.And.Not.Empty);
                Assert.That(refreshToken, Is.Not.Null.And.Not.Empty);
                Assert.That(user.RefreshToken, Is.EqualTo(refreshToken));
                Assert.That(user.RefreshTokenExpiryTime, Is.Not.Null);
                Assert.That(user.RefreshTokenExpiryTime, Is.GreaterThan(DateTime.UtcNow));
            });
        }
    }

    [Test]
    public async Task UpdateUserProfile_ShouldPersistChanges_WhenJwtIsValid()
    {
        var (factory, client) = await StartAsync();
        using (factory)
        using (client)
        {
            await RegisterAsync(client);
            var token = await LoginAsync(client);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.PutAsJsonAsync("/api/users/profile", new
            {
                firstName = "Updated",
                lastName = "Customer",
                phone = "0987654321"
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                await response.Content.ReadAsStringAsync());

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking().SingleAsync(x => x.Email == Email);
            Assert.Multiple(() =>
            {
                Assert.That(user.FirstName, Is.EqualTo("Updated"));
                Assert.That(user.LastName, Is.EqualTo("Customer"));
                Assert.That(user.Phone, Is.EqualTo("0987654321"));
            });
        }
    }

    [Test]
    public async Task UpdateDeliveryAddress_ShouldPersistTrimmedAddress_WhenJwtIsValid()
    {
        var (factory, client) = await StartAsync();
        using (factory)
        using (client)
        {
            await RegisterAsync(client);
            var token = await LoginAsync(client);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.PutAsJsonAsync("/api/profile/delivery-address", new
            {
                region = "  Чернівецька  ",
                city = "  Чернівці  ",
                streetAddress = "  Головна 10  ",
                postalCode = "  58000  "
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                await response.Content.ReadAsStringAsync());

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking().SingleAsync(x => x.Email == Email);
            Assert.Multiple(() =>
            {
                Assert.That(user.Region, Is.EqualTo("Чернівецька"));
                Assert.That(user.City, Is.EqualTo("Чернівці"));
                Assert.That(user.StreetAddress, Is.EqualTo("Головна 10"));
                Assert.That(user.PostalCode, Is.EqualTo("58000"));
            });
        }
    }
}
