using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlowerShopApi.DTOs.Auth;
using FlowerShopApi.DTOs.Discounts;
using FlowerShopApi.DTOs.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace FlowerShopApi.IntegrationTests;

/// <summary>
/// 2 E2E-сценарії (повні користувацькі флоу через API)
/// </summary>
[TestFixture]
public class OrdersE2ETests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        _factory.ResetDatabase(); // <-- Додано для ініціалізації бази
    }

    [SetUp]
    public void SetUp()
    {
        _factory.ResetDatabase();
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.TryGetProperty("accessToken", out var t)
            ? t.GetString()!
            : body.GetProperty("AccessToken").GetString()!;
    }

    [Test]
    public async Task E2E_CreateOrder_GetIt_UpdateStatus_ShouldSucceed()
    {
        var customerToken = await LoginAsync("test@example.com", "Password123!");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", customerToken);

        var createResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            DeliveryAddress = "Київ, Хрещатик 10",
            RecipientName = "Тест Юзер",
            RecipientPhone = "+380501111111",
            Comment = "E2E test",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 2 } }
        });
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.TryGetProperty("orderId", out var oid)
            ? oid.GetInt32()
            : created.GetProperty("OrderId").GetInt32();

        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var myResponse = await _client.GetAsync("/api/orders/my");
        Assert.That(myResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var adminToken = await LoginAsync("admin@example.com", "Admin123!");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminToken);

        var statusResponse = await _client.PutAsJsonAsync($"/api/orders/{orderId}/status",
            new UpdateOrderStatusDto { Status = "completed" });
        Assert.That(statusResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var finalResponse = await _client.GetAsync($"/api/orders/{orderId}");
        var finalOrder = await finalResponse.Content.ReadFromJsonAsync<JsonElement>();
        var status = finalOrder.TryGetProperty("status", out var s)
            ? s.GetString()
            : finalOrder.GetProperty("Status").GetString();
        Assert.That(status, Is.EqualTo("completed"));
    }

    [Test]
    public async Task E2E_CreateDiscount_ThenCreateOrder_ShouldSucceed()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowerShopApi.Data.AppDbContext>();
            var discountService = new FlowerShopApi.Services.DiscountService(db);
            var discount = await discountService.CreateAsync(new CreateDiscountDto
            {
                ProductId = 1,
                DiscountPercent = 15,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            });
            Assert.That(discount.DiscountPercent, Is.EqualTo(15));
        }

        var token = await LoginAsync("test@example.com", "Password123!");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var orderResponse = await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            DeliveryAddress = "Львів, пл. Ринок 1",
            RecipientName = "Олена",
            RecipientPhone = "+380671112233",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });
        Assert.That(orderResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var myOrders = await _client.GetAsync("/api/orders/my");
        Assert.That(myOrders.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var list = await myOrders.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(list.GetArrayLength(), Is.GreaterThanOrEqualTo(1));
    }
}