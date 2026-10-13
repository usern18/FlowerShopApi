
using System.Net;
using System.Net.Http.Json;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FlowerShopApi.Tests.E2ETests
{
    public class ProductE2ETests
    {
        [Fact]
        public async Task E2E01_CustomerViewsCatalogAndSelectsProduct()
        {
            // Arrange - підготовка тестового каталогу
            var products = new List<Product>
            {
                new Product
                {
                    ProductId = 5,
                    CategoryId = 1,
                    Name = "Букет півоній",
                    Price = 650,
                    StockQuantity = 7,
                    IsAvailable = true
                },
                new Product
                {
                    ProductId = 6,
                    CategoryId = 1,
                    Name = "Букет троянд",
                    Price = 450,
                    StockQuantity = 10,
                    IsAvailable = true
                }
            };

            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(products);

            mockRepository
                .Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(products[0]);

            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureTestServices(services =>
                    {
                        services.AddScoped<IProductRepository>(
                            _ => mockRepository.Object);
                    });
                });

            using var client = factory.CreateClient();

            // Крок 1 - покупець переглядає каталог
            var catalogResponse =
                await client.GetAsync("/api/products");

            Assert.Equal(
                HttpStatusCode.OK,
                catalogResponse.StatusCode);

            var catalog = await catalogResponse.Content
                .ReadFromJsonAsync<List<Product>>();

            Assert.NotNull(catalog);
            Assert.Equal(2, catalog.Count);

            // Крок 2 - покупець відкриває товар
            var productResponse =
                await client.GetAsync("/api/products/5");

            Assert.Equal(
                HttpStatusCode.OK,
                productResponse.StatusCode);

            var selectedProduct = await productResponse.Content
                .ReadFromJsonAsync<Product>();

            // Крок 3 - перевірка вибраного товару
            Assert.NotNull(selectedProduct);
            Assert.Equal(5, selectedProduct.ProductId);
            Assert.Equal("Букет півоній", selectedProduct.Name);
            Assert.Equal(650, selectedProduct.Price);
            Assert.True(selectedProduct.IsAvailable);

            // Перевіряємо виклики репозиторію
            mockRepository.Verify(
                r => r.GetAllAsync(),
                Times.Once);

            mockRepository.Verify(
                r => r.GetByIdAsync(5),
                Times.Once);
        }

        [Fact]
        public async Task E2E02_CustomerFiltersCategoryAndViewsProduct()
        {
            // Arrange - товари категорії 2
            var products = new List<Product>
    {
        new Product
        {
            ProductId = 10,
            CategoryId = 2,
            Name = "Букет тюльпанів",
            Price = 400,
            StockQuantity = 12,
            IsAvailable = true
        },
        new Product
        {
            ProductId = 11,
            CategoryId = 2,
            Name = "Букет лілій",
            Price = 550,
            StockQuantity = 6,
            IsAvailable = true
        }
    };

            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.CategoryExistsAsync(2))
                .ReturnsAsync(true);

            mockRepository
                .Setup(r => r.GetByCategoryIdAsync(2))
                .ReturnsAsync(products);

            mockRepository
                .Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(products[0]);

            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureTestServices(services =>
                    {
                        services.AddScoped<IProductRepository>(
                            _ => mockRepository.Object);
                    });
                });

            using var client = factory.CreateClient();

            // Act 1 - покупець відкриває категорію
            var categoryResponse =
                await client.GetAsync("/api/products/category/2");

            Assert.Equal(
                HttpStatusCode.OK,
                categoryResponse.StatusCode);

            var categoryProducts = await categoryResponse.Content
                .ReadFromJsonAsync<List<Product>>();

            Assert.NotNull(categoryProducts);
            Assert.Equal(2, categoryProducts.Count);
            Assert.All(categoryProducts,
                p => Assert.Equal(2, p.CategoryId));

            // Act 2 - покупець відкриває товар
            var productResponse =
                await client.GetAsync("/api/products/10");

            Assert.Equal(
                HttpStatusCode.OK,
                productResponse.StatusCode);

            var selectedProduct = await productResponse.Content
                .ReadFromJsonAsync<Product>();

            // Assert - перевіряємо товар
            Assert.NotNull(selectedProduct);
            Assert.Equal(10, selectedProduct.ProductId);
            Assert.Equal(2, selectedProduct.CategoryId);
            Assert.Equal("Букет тюльпанів", selectedProduct.Name);
            Assert.Equal(400, selectedProduct.Price);

            // Перевірка викликів репозиторію
            mockRepository.Verify(
                r => r.CategoryExistsAsync(2),
                Times.Once);

            mockRepository.Verify(
                r => r.GetByCategoryIdAsync(2),
                Times.Once);

            mockRepository.Verify(
                r => r.GetByIdAsync(10),
                Times.Once);
        }

    }
}
