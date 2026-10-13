
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowerShopApi.Tests.MockingTests
{
    public class ProductMockingTests
    {
        [Fact]
        public async Task GetByIdAsync_CallsRepositoryWithCorrectId()
        {
            // Arrange - створюємо тестовий товар
            var product = new Product
            {
                ProductId = 5,
                Name = "Лілії",
                Price = 400
            };

            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(product);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act - викликаємо сервіс
            await service.GetByIdAsync(5);

            // Assert - перевіряємо виклик репозиторію
            mockRepository.Verify(
                r => r.GetByIdAsync(5),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteAsync_CallsRemoveAndSaveChanges()
        {
            // Arrange - створюємо товар
            var product = new Product
            {
                ProductId = 3,
                Name = "Букет ромашок",
                Price = 350
            };

            var mockRepository = new Mock<IProductRepository>();

            // Репозиторій знаходить товар
            mockRepository
                .Setup(r => r.GetByIdAsync(3))
                .ReturnsAsync(product);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act - видаляємо товар
            var result = await service.DeleteAsync(3);

            // Assert - перевіряємо повідомлення
            Assert.Equal("Товар видалено", result);

            // Перевіряємо, що видалили саме наш товар
            mockRepository.Verify(
                r => r.Remove(product),
                Times.Once
            );

            // Перевіряємо збереження змін
            mockRepository.Verify(
                r => r.SaveChangesAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateAsync_CallsAddAndSaveChanges()
        {
            // Arrange - дані нового товару
            var dto = new FlowerShopApi.DTOs.Products.CreateProductDto
            {
                CategoryId = 2,
                Name = "Букет тюльпанів",
                Description = "Весняний букет",
                Price = 600,
                StockQuantity = 12,
                IsAvailable = true
            };

            var mockRepository = new Mock<IProductRepository>();

            // Категорія існує
            mockRepository
                .Setup(r => r.CategoryExistsAsync(2))
                .ReturnsAsync(true);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act - створюємо товар
            var result = await service.CreateAsync(dto);

            // Assert - перевіряємо результат
            Assert.NotNull(result);
            Assert.Equal("Букет тюльпанів", result.Name);

            // Перевіряємо, що AddAsync викликався один раз
            mockRepository.Verify(
                r => r.AddAsync(It.Is<Product>(
                    p => p.Name == "Букет тюльпанів"
                      && p.CategoryId == 2
                      && p.Price == 600
                      && p.StockQuantity == 12
                )),
                Times.Once
            );

            // Перевіряємо, що зміни зберігалися один раз
            mockRepository.Verify(
                r => r.SaveChangesAsync(),
                Times.Once
            );

            // Перевіряємо, що категорію перевірили
            mockRepository.Verify(
                r => r.CategoryExistsAsync(2),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateAsync_CallsRepositoryMethodsCorrectly()
        {
            // Arrange - товар, який потрібно оновити
            var product = new Product
            {
                ProductId = 4,
                CategoryId = 1,
                Name = "Старі троянди",
                Price = 300
            };

            var dto = new FlowerShopApi.DTOs.Products.CreateProductDto
            {
                CategoryId = 2,
                Name = "Нові троянди",
                Description = "Оновлений букет",
                Price = 550,
                StockQuantity = 8,
                IsAvailable = true
            };

            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.GetByIdAsync(4))
                .ReturnsAsync(product);

            mockRepository
                .Setup(r => r.CategoryExistsAsync(2))
                .ReturnsAsync(true);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act - оновлюємо товар
            var result = await service.UpdateAsync(4, dto);

            // Assert - перевіряємо результат
            Assert.Equal("Товар оновлено", result);

            // Перевіряємо виклик пошуку товару
            mockRepository.Verify(
                r => r.GetByIdAsync(4),
                Times.Once
            );

            // Перевіряємо виклик перевірки категорії
            mockRepository.Verify(
                r => r.CategoryExistsAsync(2),
                Times.Once
            );

            // Перевіряємо збереження змін
            mockRepository.Verify(
                r => r.SaveChangesAsync(),
                Times.Once
            );

            // Перевіряємо, що товар оновлено
            Assert.Equal("Нові троянди", product.Name);
            Assert.Equal(550, product.Price);
            Assert.Equal(2, product.CategoryId);
        }


    }
}
