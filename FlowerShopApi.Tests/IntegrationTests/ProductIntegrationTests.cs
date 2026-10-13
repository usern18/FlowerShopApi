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

namespace FlowerShopApi.Tests.IntegrationTests
{
    public class ProductIntegrationTests
    {
        [Fact]
        public async Task INT01_GetAllProducts_Returns200AndProductList()
        {
            // Arrange - підготовка тестових товарів
            var products = new List<Product>
            {
                new Product
                {
                    ProductId = 1,
                    Name = "Троянди",
                    Price = 350
                },
                new Product
                {
                    ProductId = 2,
                    Name = "Тюльпани",
                    Price = 250
                }
            };

            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(products);

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

            // Act - справжній HTTP-запит до тестового API
            var response = await client.GetAsync("/api/products");

            // Assert - перевірка відповіді
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result =
                await response.Content.ReadFromJsonAsync<List<Product>>();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Троянди", result[0].Name);
            Assert.Equal("Тюльпани", result[1].Name);

            mockRepository.Verify(
                r => r.GetAllAsync(),
                Times.Once);
        }

        [Fact]
        public async Task INT02_GetProductById_ReturnsCorrectProduct()
        {
            // Arrange - створюємо тестовий товар
            var product = new Product
            {
                ProductId = 5,
                Name = "Букет півоній",
                Price = 650,
                StockQuantity = 7,
                CategoryId = 1
            };

            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(product);

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

            // Act - надсилаємо HTTP-запит
            var response = await client.GetAsync("/api/products/5");

            // Assert - перевіряємо результат
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result =
                await response.Content.ReadFromJsonAsync<Product>();

            Assert.NotNull(result);
            Assert.Equal(5, result.ProductId);
            Assert.Equal("Букет півоній", result.Name);
            Assert.Equal(650, result.Price);
            Assert.Equal(7, result.StockQuantity);

            mockRepository.Verify(
                r => r.GetByIdAsync(5),
                Times.Once);
        }

        [Fact]
        public async Task INT03_GetProductById_NotFound_Returns404()
        {
            // Arrange - товар з ID 99999 не існує
            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.GetByIdAsync(99999))
                .ReturnsAsync((Product)null!);

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

            // Act - запитуємо неіснуючий товар
            var response = await client.GetAsync("/api/products/99999");

            // Assert - перевіряємо HTTP-статус
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // Перевіряємо JSON-відповідь
            using var json = System.Text.Json.JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

            var root = json.RootElement;

            Assert.Equal(404, root.GetProperty("statusCode").GetInt32());
            Assert.Equal(
                "Товар не знайдено",
                root.GetProperty("message").GetString());

            // Репозиторій викликається один раз
            mockRepository.Verify(
                r => r.GetByIdAsync(99999),
                Times.Once);
        }

        [Fact]
        public async Task INT04_CreateProduct_WithoutAuthorization_Returns401()
        {
            // Arrange - підготовка підробленого репозиторію
            var mockRepository = new Mock<IProductRepository>();

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

            // Дані нового товару
            var newProduct = new
            {
                Name = "Букет ромашок",
                Description = "Тестовий букет",
                Price = 300,
                StockQuantity = 10,
                CategoryId = 1,
                IsAvailable = true
            };

            // Act - POST-запит без токена авторизації
            var response = await client.PostAsJsonAsync(
                "/api/products", newProduct);

            // Assert - доступ має бути заборонений
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);

            // Переконуємося, що товар не додавався
            mockRepository.Verify(
                r => r.AddAsync(It.IsAny<Product>()),
                Times.Never);

            mockRepository.Verify(
                r => r.SaveChangesAsync(),
                Times.Never);
        }

    }
}
