using System.Security.Claims;
using FlowerShopApi.Data;
using FlowerShopApi.DTOs.Discounts;
using FlowerShopApi.DTOs.Orders;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowerShopApi.Tests.Services;

/// <summary>
/// 4 Exception-тести: неіснуюче замовлення, порожнє замовлення, недостатній stock, невалідна знижка
/// </summary>
[TestFixture]
public class OrderDiscountExceptionTests
{
    private Mock<IOrderRepository> _repo = null!;
    private Mock<IHttpContextAccessor> _http = null!;
    private Mock<ILogger<OrderService>> _logger = null!;
    private OrderService _orderService = null!;
    private AppDbContext _db = null!;
    private DiscountService _discountService = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IOrderRepository>();
        _http = new Mock<IHttpContextAccessor>();
        _logger = new Mock<ILogger<OrderService>>();
        _orderService = new OrderService(_repo.Object, _http.Object, _logger.Object);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _discountService = new DiscountService(_db);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "Test");
        _http.Setup(h => h.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        });
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public void GetByIdAsync_ShouldThrowNotFoundException_WhenOrderDoesNotExist()
    {
        _repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Order?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _orderService.GetByIdAsync(999));
        Assert.That(ex!.Message, Is.EqualTo("Замовлення не знайдено"));
    }

    [Test]
    public void CreateAsync_ShouldThrowBadRequestException_WhenItemsListIsEmpty()
    {
        var dto = new CreateOrderDto
        {
            DeliveryAddress = "Адреса",
            RecipientName = "Ім'я",
            RecipientPhone = "+38050",
            Items = new List<CreateOrderItemDto>()
        };

        var ex = Assert.ThrowsAsync<BadRequestException>(() => _orderService.CreateAsync(dto));
        Assert.That(ex!.Message, Is.EqualTo("Замовлення не може бути порожнім"));
    }

    [Test]
    public void CreateAsync_ShouldThrowBadRequestException_WhenInsufficientStock()
    {
        var product = new Product
        {
            ProductId = 1,
            Name = "Рідкісна квітка",
            Price = 500,
            StockQuantity = 1
        };
        _repo.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(product);

        var dto = new CreateOrderDto
        {
            DeliveryAddress = "Адреса",
            RecipientName = "Ім'я",
            RecipientPhone = "+38050",
            Items = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 10 } }
        };

        var ex = Assert.ThrowsAsync<BadRequestException>(() => _orderService.CreateAsync(dto));
        Assert.That(ex!.Message, Does.Contain("Недостатня кількість"));
    }

    [Test]
    public void CreateDiscount_ShouldThrowBadRequestException_WhenProductDoesNotExist()
    {
        var dto = new CreateDiscountDto
        {
            ProductId = 999,
            DiscountPercent = 15,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        var ex = Assert.ThrowsAsync<BadRequestException>(() => _discountService.CreateAsync(dto));
        Assert.That(ex!.Message, Is.EqualTo("Товар не існує"));
    }
}
