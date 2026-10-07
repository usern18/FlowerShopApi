using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlowerShopApi.DTOs.Auth;
using FlowerShopApi.DTOs.Orders;

namespace FlowerShopApi.IntegrationTests;

/// <summary>
/// 4 REST API тести для Orders endpoints
/// </summary>
[TestFixture]
public class OrdersApiTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private string _customerToken = null!;
    private string _adminToken = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        _factory.ResetDatabase();

        _customerToken = await LoginAsync("test@example.com", "Password123!");
        _adminToken = await LoginAsync("admin@example.com", "Admin123!");
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

    private void UseToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    [Test]
    public async Task CreateOrder_ShouldReturn201_WhenAuthenticatedAndValid()
    {
        UseToken(await LoginAsync("test@example.com", "Password123!"));

        var dto = new CreateOrderDto
        {
            DeliveryAddress = "Київ, Хрещатик 10",
            RecipientName = "Тест",
            RecipientPhone = "+380501111111",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        };

        var response = await _client.PostAsJsonAsync("/api/orders", dto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task GetMyOrders_ShouldReturn200_WhenAuthenticated()
    {
        UseToken(await LoginAsync("test@example.com", "Password123!"));

        await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            DeliveryAddress = "Addr",
            RecipientName = "Name",
            RecipientPhone = "Phone",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });

        var response = await _client.GetAsync("/api/orders/my");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetOrderById_ShouldReturn200_WhenOrderExists()
    {
        UseToken(await LoginAsync("test@example.com", "Password123!"));

        var create = await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            DeliveryAddress = "Addr",
            RecipientName = "Name",
            RecipientPhone = "Phone",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.TryGetProperty("orderId", out var oid)
            ? oid.GetInt32()
            : created.GetProperty("OrderId").GetInt32();

        var response = await _client.GetAsync($"/api/orders/{id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task UpdateOrderStatus_ShouldReturn200_WhenAdmin()
    {
        UseToken(await LoginAsync("test@example.com", "Password123!"));
        var create = await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto
        {
            DeliveryAddress = "Addr",
            RecipientName = "Name",
            RecipientPhone = "Phone",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.TryGetProperty("orderId", out var oid)
            ? oid.GetInt32()
            : created.GetProperty("OrderId").GetInt32();

        UseToken(await LoginAsync("admin@example.com", "Admin123!"));
        var response = await _client.PutAsJsonAsync($"/api/orders/{id}/status",
            new UpdateOrderStatusDto { Status = "completed" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
