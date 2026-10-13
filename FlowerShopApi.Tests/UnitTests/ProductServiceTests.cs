
using FlowerShopApi.DTOs.Products;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowerShopApi.Tests.UnitTests
{
    public class ProductServiceTests
    {
        [Fact]
        public async Task GetAllAsync_ReturnsAllProducts()
        {
            // Arrange - готуємо тестові дані
            var products = new List<Product>
            {
                new Product
                {
                    ProductId = 1,
                    Name = "Троянди",
                    Price = 500
                },
                new Product
                {
                    ProductId = 2,
                    Name = "Тюльпани",
                    Price = 300
                }
            };

            // Створюємо підроблений репозиторій
            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(products);

            // Створюємо справжній сервіс
            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act - викликаємо метод
            var result = await service.GetAllAsync();

            // Assert - перевіряємо результат
            Assert.Equal(2, result.Count);
            Assert.Equal("Троянди", result[0].Name);
            Assert.Equal("Тюльпани", result[1].Name);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectProduct()
        {
            // Arrange - створюємо товар
            var product = new Product
            {
                ProductId = 1,
                Name = "Орхідея",
                Price = 700
            };

            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(product);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act - шукаємо товар за ID
            var result = await service.GetByIdAsync(1);

            // Assert - перевіряємо результат
            Assert.NotNull(result);
            Assert.Equal(1, result.ProductId);
            Assert.Equal("Орхідея", result.Name);
            Assert.Equal(700, result.Price);
        }

        [Fact]
        public async Task GetByCategoryIdAsync_ReturnsProductsFromCategory()
        {
            // Arrange - товари однієї категорії
            var products = new List<Product>
    {
        new Product
        {
            ProductId = 1,
            Name = "Червоні троянди",
            Price = 500,
            CategoryId = 1
        },
        new Product
        {
            ProductId = 2,
            Name = "Білі троянди",
            Price = 450,
            CategoryId = 1
        }
    };

            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.GetByCategoryIdAsync(1))
                .ReturnsAsync(products);
            mockRepository
                .Setup(r => r.CategoryExistsAsync(1))
                .ReturnsAsync(true);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act - отримуємо товари категорії 1
            var result = await service.GetByCategoryIdAsync(1);

            // Assert - перевіряємо результат
            Assert.Equal(2, result.Count);
            Assert.All(result, p => Assert.Equal(1, p.CategoryId));
        }


        [Fact]
        public async Task CreateAsync_CreatesProductSuccessfully()
        {
            // Arrange - дані нового товару
            var dto = new CreateProductDto
            {
                CategoryId = 1,
                Name = "Букет півоній",
                Description = "Рожеві півонії",
                Price = 850,
                StockQuantity = 10,
                IsAvailable = true
            };

            var mockRepository = new Mock<IProductRepository>();

            // Категорія існує
            mockRepository
                .Setup(r => r.CategoryExistsAsync(1))
                .ReturnsAsync(true);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act - створюємо товар
            var result = await service.CreateAsync(dto);

            // Assert - перевіряємо результат
            Assert.NotNull(result);
            Assert.Equal("Букет півоній", result.Name);
            Assert.Equal(850, result.Price);
            Assert.Equal(10, result.StockQuantity);
            Assert.Equal(1, result.CategoryId);
            Assert.True(result.IsAvailable);
            Assert.True(result.CreatedAt <= DateTime.UtcNow);

            // Перевіряємо, що товар передано репозиторію
            mockRepository.Verify(
                r => r.AddAsync(It.Is<Product>(
                    p => p.Name == "Букет півоній"
                      && p.Price == 850
                      && p.CategoryId == 1
                )),
                Times.Once
            );

            // Перевіряємо збереження змін
            mockRepository.Verify(
                r => r.SaveChangesAsync(),
                Times.Once
            );
        }


        [Fact]
        public async Task UpdateAsync_UpdatesProductSuccessfully()
        {
            // Arrange - товар до оновлення
            var product = new Product
            {
                ProductId = 1,
                CategoryId = 1,
                Name = "Старий букет",
                Price = 300,
                StockQuantity = 5,
                IsAvailable = true
            };

            // Нові дані товару
            var dto = new CreateProductDto
            {
                CategoryId = 1,
                Name = "Новий букет",
                Description = "Оновлений букет квітів",
                Price = 650,
                StockQuantity = 15,
                IsAvailable = true
            };

            var mockRepository = new Mock<IProductRepository>();

            // Товар існує
            mockRepository
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(product);

            // Категорія існує
            mockRepository
                .Setup(r => r.CategoryExistsAsync(1))
                .ReturnsAsync(true);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act - оновлюємо товар
            var result = await service.UpdateAsync(1, dto);

            // Assert - перевіряємо нові дані
            Assert.Equal("Товар оновлено", result);
            Assert.Equal("Новий букет", product.Name);
            Assert.Equal(650, product.Price);
            Assert.Equal(15, product.StockQuantity);
            Assert.Equal(1, product.CategoryId);
            Assert.Equal("Оновлений букет квітів", product.Description);

            // Перевіряємо збереження змін
            mockRepository.Verify(
                r => r.SaveChangesAsync(),
                Times.Once
            );
        }

    }
}
