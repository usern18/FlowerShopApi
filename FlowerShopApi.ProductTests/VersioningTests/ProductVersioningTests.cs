
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FlowerShopApi.Tests.VersioningTests;

public class ProductVersioningTests
{
    private static Product CreateTestProduct()
    {
        return new Product
        {
            ProductId = 5,
            CategoryId = 1,
            Name = "Букет троянд",
            Price = 450,
            StockQuantity = 7,
            IsAvailable = true
        };
    }

    [Theory]
    [InlineData("/api/products")]
    [InlineData("/api/v1/products")]
    public async Task V1_AndLegacy_ShouldReturnOldProductFormat(
        string url)
    {
        // Arrange
        var product = CreateTestProduct();
        var mockRepository = new Mock<IProductRepository>();

        mockRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Product> { product });

        using var factory = new ProductTestFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IProductRepository>(
                        _ => mockRepository.Object);
                });
            });

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var item = json.RootElement[0];

        Assert.Equal(5, item.GetProperty("productId").GetInt32());
        Assert.Equal("Букет троянд",
            item.GetProperty("name").GetString());
        Assert.Equal(7,
            item.GetProperty("stockQuantity").GetInt32());

        mockRepository.Verify(
            r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task V2_GetAll_ShouldReturnNewDtoFormat()
    {
        // Arrange
        var mockRepository = new Mock<IProductRepository>();

        mockRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(
                new List<Product> { CreateTestProduct() });

        using var factory = new ProductTestFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IProductRepository>(
                        _ => mockRepository.Object);
                });
            });

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v2/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var item = json.RootElement[0];

        Assert.Equal(5, item.GetProperty("id").GetInt32());
        Assert.Equal("Букет троянд",
            item.GetProperty("name").GetString());
        Assert.Equal(450m,
            item.GetProperty("price").GetDecimal());
        Assert.True(item.GetProperty("inStock").GetBoolean());
        Assert.Equal(7,
            item.GetProperty("availableQuantity").GetInt32());

        // Поля старої моделі не повинно бути у v2.
        Assert.False(
            item.TryGetProperty("stockQuantity", out _));
    }

    [Fact]
    public async Task V2_GetById_ShouldReturnProduct()
    {
        // Arrange
        var mockRepository = new Mock<IProductRepository>();

        mockRepository
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(CreateTestProduct());

        using var factory = new ProductTestFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IProductRepository>(
                        _ => mockRepository.Object);
                });
            });

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/v2/products/5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var item = json.RootElement;

        Assert.Equal(5, item.GetProperty("id").GetInt32());
        Assert.True(item.GetProperty("inStock").GetBoolean());
        Assert.Equal(7,
            item.GetProperty("availableQuantity").GetInt32());
    }

    [Theory]
    [InlineData("/api/v1/products/99999")]
    [InlineData("/api/v2/products/99999")]
    public async Task ProductNotFound_ShouldReturn404(
        string url)
    {
        // Arrange
        var mockRepository = new Mock<IProductRepository>();

        mockRepository
            .Setup(r => r.GetByIdAsync(99999))
            .ReturnsAsync((Product)null!);

        using var factory = new ProductTestFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IProductRepository>(
                        _ => mockRepository.Object);
                });
            });

        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound,
            response.StatusCode);
    }
}
