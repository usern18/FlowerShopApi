using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlowerShopApi.Data;
using FlowerShopApi.DTOs.Auth;
using FlowerShopApi.DTOs.Discounts;
using FlowerShopApi.DTOs.Orders;
using FlowerShopApi.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FlowerShopApi.IntegrationTests;

/// <summary>
/// 4 Integration-тести: OrdersController → OrderService → Repository (+ Discount)
/// </summary>
[TestFixture]
public class OrdersIntegrationTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        _factory.ResetDatabase(); 
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
    public async Task CreateOrder_ShouldDecreaseProductStock_InDatabase()
    {
        var token = await LoginAsync("test@example.com", "Password123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        int stockBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            stockBefore = db.Products.Single(p => p.ProductId == 1).StockQuantity;
        }

        await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            DeliveryAddress = "Addr",
            RecipientName = "Name",
            RecipientPhone = "Phone",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 3 } }
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stockAfter = db.Products.Single(p => p.ProductId == 1).StockQuantity;
            Assert.That(stockAfter, Is.EqualTo(stockBefore - 3));
            Assert.That(db.Orders.Count(), Is.EqualTo(1));
            Assert.That(db.OrderItems.Count(), Is.EqualTo(1));
        }
    }

    [Test]
    public async Task GetMyOrders_ShouldReturnOnlyOrdersOfCurrentUser()
    {
        var token = await LoginAsync("test@example.com", "Password123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            DeliveryAddress = "Addr",
            RecipientName = "Name",
            RecipientPhone = "Phone",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        var response = await _client.GetAsync("/api/orders/my");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var orders = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(orders.GetArrayLength(), Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateStatus_ShouldPersistNewStatus_InDatabase()
    {
        var customerToken = await LoginAsync("test@example.com", "Password123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);

        var create = await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            DeliveryAddress = "Addr",
            RecipientName = "Name",
            RecipientPhone = "Phone",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.TryGetProperty("orderId", out var oid)
            ? oid.GetInt32()
            : created.GetProperty("OrderId").GetInt32();

        var adminToken = await LoginAsync("admin@example.com", "Admin123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        await _client.PutAsJsonAsync($"/api/orders/{orderId}/status",
            new UpdateOrderStatusDto { Status = "shipped" });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = db.Orders.Single(o => o.OrderId == orderId);
        Assert.That(order.Status, Is.EqualTo("shipped"));
    }

    [Test]
    public async Task CreateDiscount_ShouldPersistInDatabase_WhenProductExists()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new FlowerShopApi.Services.DiscountService(db);

        var discount = await service.CreateAsync(new CreateDiscountDto
        {
            ProductId = 1,
            DiscountPercent = 20,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10)
        });

        Assert.That(discount.DiscountPercent, Is.EqualTo(20));
        Assert.That(db.Discounts.Count(), Is.EqualTo(1));
    }
}