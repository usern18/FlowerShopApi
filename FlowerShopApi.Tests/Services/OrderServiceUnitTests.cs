using System.Security.Claims;
using FlowerShopApi.DTOs.Orders;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowerShopApi.Tests.Services;

/// <summary>
/// 5 Unit-тестів: життєвий цикл замовлення (Create, GetMy, GetAll, GetById, UpdateStatus)
/// </summary>
[TestFixture]
public class OrderServiceUnitTests
{
    private Mock<IOrderRepository> _repo = null!;
    private Mock<IHttpContextAccessor> _http = null!;
    private Mock<ILogger<OrderService>> _logger = null!;
    private OrderService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IOrderRepository>();
        _http = new Mock<IHttpContextAccessor>();
        _logger = new Mock<ILogger<OrderService>>();
        _sut = new OrderService(_repo.Object, _http.Object, _logger.Object);
    }

    private void AuthAs(int userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "Test");
        _http.Setup(h => h.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        });
    }

    [Test]
    public async Task CreateAsync_ShouldCreateOrderWithCorrectTotal_WhenValidItems()
    {
        AuthAs(10);
        var product = new Product
        {
            ProductId = 1,
            Name = "Троянда",
            Price = 150m,
            StockQuantity = 20
        };
        var dto = new CreateOrderDto
        {
            DeliveryAddress = "Київ, вул. Хрещатик 1",
            RecipientName = "Іван",
            RecipientPhone = "+380501112233",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = 1, Quantity = 2 }
            }
        };
        _repo.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(product);
        _repo.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var order = await _sut.CreateAsync(dto);

        Assert.That(order.UserId, Is.EqualTo(10));
        Assert.That(order.Status, Is.EqualTo("new"));
        Assert.That(order.TotalAmount, Is.EqualTo(300m));
        Assert.That(order.Items, Has.Count.EqualTo(1));
        Assert.That(product.StockQuantity, Is.EqualTo(18));
    }

    [Test]
    public async Task GetMyOrdersAsync_ShouldReturnOnlyCurrentUserOrders()
    {
        AuthAs(7);
        var orders = new List<Order>
        {
            new() { OrderId = 1, UserId = 7, TotalAmount = 100 },
            new() { OrderId = 2, UserId = 7, TotalAmount = 250 }
        };
        _repo.Setup(r => r.GetByUserIdAsync(7)).ReturnsAsync(orders);

        var result = await _sut.GetMyOrdersAsync();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(o => o.UserId == 7), Is.True);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllOrders()
    {
        var orders = new List<Order>
        {
            new() { OrderId = 1 },
            new() { OrderId = 2 },
            new() { OrderId = 3 }
        };
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(orders);

        var result = await _sut.GetAllAsync();

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnOrder_WhenOrderExists()
    {
        var order = new Order { OrderId = 5, Status = "new", TotalAmount = 500 };
        _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(order);

        var result = await _sut.GetByIdAsync(5);

        Assert.That(result.OrderId, Is.EqualTo(5));
        Assert.That(result.Status, Is.EqualTo("new"));
    }

    [Test]
    public async Task UpdateStatusAsync_ShouldChangeStatus_WhenOrderExists()
    {
        var order = new Order { OrderId = 1, Status = "new" };
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var message = await _sut.UpdateStatusAsync(1, new UpdateOrderStatusDto { Status = "completed" });

        Assert.That(message, Is.EqualTo("Статус замовлення оновлено"));
        Assert.That(order.Status, Is.EqualTo("completed"));
    }
}
