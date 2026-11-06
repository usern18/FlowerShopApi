using System.Security.Claims;
using FlowerShopApi.DTOs.Orders;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowerShopApi.Tests.Services;

/// <summary>
/// 4 Mocking-тести: перевірка взаємодії OrderService з IOrderRepository
/// </summary>
[TestFixture]
public class OrderServiceMockingTests
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

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5")
        }, "Test");
        _http.Setup(h => h.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        });
    }

    [Test]
    public async Task CreateAsync_ShouldCallGetProductAddAndSave_OnceEach()
    {
        var product = new Product { ProductId = 1, Name = "A", Price = 100, StockQuantity = 10 };
        _repo.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(product);
        _repo.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new CreateOrderDto
        {
            DeliveryAddress = "Addr",
            RecipientName = "Name",
            RecipientPhone = "Phone",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 1 } }
        };

        await _sut.CreateAsync(dto);

        _repo.Verify(r => r.GetProductByIdAsync(1), Times.Once);
        _repo.Verify(r => r.AddAsync(It.Is<Order>(o => o.UserId == 5 && o.TotalAmount == 100)), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task GetMyOrdersAsync_ShouldCallGetByUserId_WithCurrentUserId()
    {
        _repo.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync(new List<Order>());

        await _sut.GetMyOrdersAsync();

        _repo.Verify(r => r.GetByUserIdAsync(5), Times.Once);
        _repo.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Test]
    public async Task UpdateStatusAsync_ShouldCallSaveChanges_Once()
    {
        var order = new Order { OrderId = 3, Status = "new" };
        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(order);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.UpdateStatusAsync(3, new UpdateOrderStatusDto { Status = "shipped" });

        _repo.Verify(r => r.GetByIdAsync(3), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _repo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Test]
    public async Task GetByIdAsync_ShouldNotCallSaveChanges_WhenOnlyReading()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Order { OrderId = 1 });

        await _sut.GetByIdAsync(1);

        _repo.Verify(r => r.GetByIdAsync(1), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        _repo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }
}
