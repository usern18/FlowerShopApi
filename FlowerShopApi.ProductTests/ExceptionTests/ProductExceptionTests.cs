
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowerShopApi.Tests.ExceptionTests
{
    public class ProductExceptionTests
    {
        [Fact]
        public async Task GetByIdAsync_ProductNotFound_ThrowsNotFoundException()
        {
            // Arrange - товар з ID 999 не існує
            var mockRepository = new Mock<IProductRepository>();

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act & Assert - очікуємо помилку
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => service.GetByIdAsync(999)
            );

            Assert.Equal("Товар не знайдено", exception.Message);
        }

        [Fact]
        public async Task GetByCategoryIdAsync_CategoryNotFound_ThrowsNotFoundException()
        {
            // Arrange - категорія з ID 999 не існує
            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.CategoryExistsAsync(999))
                .ReturnsAsync(false);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act & Assert - перевіряємо виняток
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => service.GetByCategoryIdAsync(999)
            );

            // Перевіряємо текст помилки
            Assert.Equal("Категорія не існує", exception.Message);

            // Перевіряємо, що товари не запитувалися
            mockRepository.Verify(
                r => r.GetByCategoryIdAsync(It.IsAny<int>()),
                Times.Never
            );
        }


        [Fact]
        public async Task CreateAsync_CategoryNotFound_ThrowsBadRequestException()
        {
            // Arrange - дані нового товару
            var dto = new FlowerShopApi.DTOs.Products.CreateProductDto
            {
                CategoryId = 999,
                Name = "Букет троянд",
                Description = "Червоні троянди",
                Price = 500,
                StockQuantity = 10,
                IsAvailable = true
            };

            var mockRepository = new Mock<IProductRepository>();

            // Категорія 999 не існує
            mockRepository
                .Setup(r => r.CategoryExistsAsync(999))
                .ReturnsAsync(false);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act & Assert - очікуємо BadRequestException
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                () => service.CreateAsync(dto)
            );

            Assert.Equal("Категорія не існує", exception.Message);

            // Перевіряємо, що товар не додавали
            mockRepository.Verify(
                r => r.AddAsync(It.IsAny<FlowerShopApi.Models.Product>()),
                Times.Never
            );

            // Перевіряємо, що зміни не зберігалися
            mockRepository.Verify(
                r => r.SaveChangesAsync(),
                Times.Never
            );
        }


        [Fact]
        public async Task DeleteAsync_ProductNotFound_ThrowsNotFoundException()
        {
            // Arrange - товар з ID 999 не існує
            var mockRepository = new Mock<IProductRepository>();

            mockRepository
                .Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Product?)null);

            var service = new ProductService(
                mockRepository.Object,
                NullLogger<ProductService>.Instance
            );

            // Act & Assert - очікуємо NotFoundException
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => service.DeleteAsync(999)
            );

            // Перевіряємо повідомлення про помилку
            Assert.Equal("Товар не знайдено", exception.Message);

            // Перевіряємо, що видалення не виконувалося
            mockRepository.Verify(
                r => r.Remove(It.IsAny<Product>()),
                Times.Never
            );

            // Перевіряємо, що зміни не зберігалися
            mockRepository.Verify(
                r => r.SaveChangesAsync(),
                Times.Never
            );
        }

    }
}
