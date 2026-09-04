using FlowerShopApi.DTOs.ProductImages;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;

namespace FlowerShopApi.Services
{
    public class ProductImageService
    {
        private readonly IProductImageRepository _productImageRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductImageService> _logger;

        public ProductImageService(
            IProductImageRepository productImageRepository,
            IWebHostEnvironment environment,
            ILogger<ProductImageService> logger)
        {
            _productImageRepository = productImageRepository;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ProductImage> UploadAsync(int productId, UploadProductImageDto dto)
        {
            if (!await _productImageRepository.ProductExistsAsync(productId))
                throw new NotFoundException("Товар не знайдено");

            if (dto.File == null || dto.File.Length == 0)
                throw new BadRequestException("Файл не вибрано");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (!allowedExtensions.Contains(extension))
                throw new BadRequestException("Дозволені тільки файли JPG, JPEG, PNG, WEBP");

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            var image = new ProductImage
            {
                ProductId = productId,
                ImageUrl = $"/uploads/products/{uniqueFileName}",
                IsMain = dto.IsMain
            };

            await _productImageRepository.AddAsync(image);
            await _productImageRepository.SaveChangesAsync();

            _logger.LogInformation("Завантажено зображення для товару {ProductId}: {FileName}", productId, uniqueFileName);

            return image;
        }

        public async Task<List<ProductImage>> GetByProductIdAsync(int productId)
        {
            if (!await _productImageRepository.ProductExistsAsync(productId))
                throw new NotFoundException("Товар не знайдено");

            return await _productImageRepository.GetByProductIdAsync(productId);
        }

        public async Task<string> DeleteAsync(int imageId)
        {
            var image = await _productImageRepository.GetByIdAsync(imageId);
            if (image == null)
                throw new NotFoundException("Зображення не знайдено");

            var physicalPath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            _productImageRepository.Remove(image);
            await _productImageRepository.SaveChangesAsync();

            _logger.LogInformation("Видалено зображення {ImageId}", imageId);

            return "Зображення видалено";
        }
    }
}