using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlowerShopApi.Tests.Api;
using NUnit.Framework;

namespace FlowerShopApi.Tests.E2E;

[TestFixture]
public sealed class UserAccountE2ETests
{
    private const string Password = "Password123!";

    private static async Task<(FlowerShopApiTestFactory Factory, HttpClient Client)> StartAsync()
    {
        var factory = new FlowerShopApiTestFactory();
        var client = factory.CreateClient();
        await factory.SeedCustomerRoleAsync();
        return (factory, client);
    }

    private static async Task RegisterAsync(HttpClient client, string email, string phone)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "Customer",
            email,
            phone,
            password = Password
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            await response.Content.ReadAsStringAsync());
    }

    private static async Task<string> LoginAndAuthorizeAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = Password
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            await response.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        Assert.That(token, Is.Not.Null.And.Not.Empty);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return token!;
    }

    [Test]
    public async Task User_ShouldRegisterLoginAndRetrieveOwnProfile()
    {
        const string email = "e2e-register@example.com";
        var (factory, client) = await StartAsync();
        using (factory)
        using (client)
        {
            // Реєстрація -> вхід -> запит до захищеного endpoint -> перевірка користувача.
            await RegisterAsync(client, email, "0991234567");
            await LoginAndAuthorizeAsync(client, email);

            using var response = await client.GetAsync("/api/auth/me");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                await response.Content.ReadAsStringAsync());
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var user = json.RootElement;
            Assert.Multiple(() =>
            {
                Assert.That(user.GetProperty("userId").GetInt32(), Is.GreaterThan(0));
                Assert.That(user.GetProperty("email").GetString(), Is.EqualTo(email));
                Assert.That(user.GetProperty("firstName").GetString(), Is.EqualTo("Test"));
                Assert.That(user.GetProperty("lastName").GetString(), Is.EqualTo("Customer"));
            });
        }
    }

    [Test]
    public async Task User_ShouldLoginUpdateProfileAndRetrieveUpdatedData()
    {
        const string email = "e2e-update@example.com";
        var (factory, client) = await StartAsync();
        using (factory)
        using (client)
        {
            // Реєстрація потрібна для підготовки незалежного тестового користувача.
            await RegisterAsync(client, email, "0997654321");
            await LoginAndAuthorizeAsync(client, email);

            // Змінюємо профіль через справжній HTTP endpoint.
            using var updateResponse = await client.PutAsJsonAsync("/api/profile/personal-info", new
            {
                firstName = "  Updated  ",
                lastName = "  Customer  ",
                middleName = "  New  ",
                phone = "0997654321"
            });
            Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                await updateResponse.Content.ReadAsStringAsync());

            // Окремим HTTP-запитом читаємо збережені дані, а не лише результат PUT.
            using var getResponse = await client.GetAsync("/api/profile");
            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                await getResponse.Content.ReadAsStringAsync());
            using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
            var profile = json.RootElement;
            Assert.Multiple(() =>
            {
                Assert.That(profile.GetProperty("email").GetString(), Is.EqualTo(email));
                Assert.That(profile.GetProperty("firstName").GetString(), Is.EqualTo("Updated"));
                Assert.That(profile.GetProperty("lastName").GetString(), Is.EqualTo("Customer"));
                Assert.That(profile.GetProperty("middleName").GetString(), Is.EqualTo("New"));
            });
        }
    }
}
