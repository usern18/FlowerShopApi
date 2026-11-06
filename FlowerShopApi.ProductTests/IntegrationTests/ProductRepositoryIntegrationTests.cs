
using FlowerShopApi.Data;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FlowerShopApi.ProductTests.IntegrationTests;

public class ProductRepositoryIntegrationTests
{
    [Fact]
    public async Task GetByCategoryIdAsync_ShouldReturnOnlyProductsFromSpecifiedCategory()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        context.Categories.AddRange(
            new Category
            {
                CategoryId = 1,
                CategoryName = "Троянди"
            },
            new Category
            {
                CategoryId = 2,
                CategoryName = "Тюльпани"
            }
        );

        context.Products.AddRange(
            new Product
            {
                ProductId = 1,
                CategoryId = 1,
                Name = "Червоні троянди",
                Description = "Букет червоних троянд",
                Price = 100m,
                StockQuantity = 10,
                IsAvailable = true
            },
            new Product
            {
                ProductId = 2,
                CategoryId = 2,
                Name = "Жовті тюльпани",
                Description = "Букет жовтих тюльпанів",
                Price = 80m,
                StockQuantity = 5,
                IsAvailable = true
            }
        );

        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);

        // Act
        var result = await repository.GetByCategoryIdAsync(1);

        // Assert
        Assert.Single(result);
        Assert.Equal("Червоні троянди", result[0].Name);
        Assert.Equal(1, result[0].CategoryId);
        Assert.NotNull(result[0].Category);
        Assert.Equal("Троянди", result[0].Category!.CategoryName);
    }
}
