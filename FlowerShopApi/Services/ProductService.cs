
using System.ComponentModel.DataAnnotations;
using FlowerShopApi.DTOs.Products;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;

namespace FlowerShopApi.Services
{
    public class ProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IProductRepository productRepository,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            _logger.LogInformation("Отримання всіх товарів");
            return await _productRepository.GetAllAsync();
        }

        public async Task<List<Product>> GetOutOfStockAsync()
        {
            _logger.LogInformation("Отримання товарів, яких немає в наявності");
            return await _productRepository.GetOutOfStockAsync();
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            _logger.LogInformation("Отримання товару з id {Id}", id);

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException("Товар не знайдено");

            return product;
        }

        public async Task<List<Product>> GetByCategoryIdAsync(int categoryId)
        {
            _logger.LogInformation(
                "Отримання товарів за id категорії {CategoryId}",
                categoryId);

            var categoryExists =
                await _productRepository.CategoryExistsAsync(categoryId);

            if (!categoryExists)
                throw new NotFoundException("Категорія не існує");

            return await _productRepository.GetByCategoryIdAsync(categoryId);
        }

        public async Task<Product> CreateAsync(CreateProductDto dto)
        {
            _logger.LogInformation("Створення товару {Name}", dto.Name);

            ValidateProductDto(dto);

            int categoryId = dto.CategoryId!.Value;

            var categoryExists =
                await _productRepository.CategoryExistsAsync(categoryId);

            if (!categoryExists)
                throw new BadRequestException("Категорія не існує");

            var product = new Product
            {
                CategoryId = categoryId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price!.Value,
                StockQuantity = dto.StockQuantity!.Value,
                IsAvailable = dto.IsAvailable ?? true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _productRepository.AddAsync(product);
            await _productRepository.SaveChangesAsync();

            return product;
        }

        public async Task<string> UpdateAsync(int id, CreateProductDto dto)
        {
            _logger.LogInformation("Оновлення товару з id {Id}", id);

            ValidateProductDto(dto);

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException("Товар не знайдено");

            int categoryId = dto.CategoryId!.Value;

            var categoryExists =
                await _productRepository.CategoryExistsAsync(categoryId);

            if (!categoryExists)
                throw new BadRequestException("Категорія не існує");

            product.CategoryId = categoryId;
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price!.Value;
            product.StockQuantity = dto.StockQuantity!.Value;
            product.IsAvailable = dto.IsAvailable ?? true;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.SaveChangesAsync();

            return "Товар оновлено";
        }

        public async Task<string> DeleteAsync(int id)
        {
            _logger.LogInformation("Видалення товару з id {Id}", id);

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException("Товар не знайдено");

            _productRepository.Remove(product);
            await _productRepository.SaveChangesAsync();

            return "Товар видалено";
        }

        private static void ValidateProductDto(CreateProductDto dto)
        {
            if (dto == null)
                throw new BadRequestException("Дані товару не передано");

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(dto);

            bool isValid = Validator.TryValidateObject(
                dto,
                validationContext,
                validationResults,
                validateAllProperties: true);

            if (!isValid)
            {
                var errors = string.Join(
                    "; ",
                    validationResults.Select(r => r.ErrorMessage));

                throw new BadRequestException(errors);
            }
        }
    }
}
