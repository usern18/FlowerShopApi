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

        public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            _logger.LogInformation("Отримання всіх товарів");
            return await _productRepository.GetAllAsync();
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
            _logger.LogInformation("Отримання товарів за id категорії {CategoryId}", categoryId);

            var categoryExists = await _productRepository.CategoryExistsAsync(categoryId);
            if (!categoryExists)
                throw new NotFoundException("Категорія не існує");

            return await _productRepository.GetByCategoryIdAsync(categoryId);
        }

        public async Task<Product> CreateAsync(CreateProductDto dto)
        {
            _logger.LogInformation("Створення товару {Name}", dto.Name);

            var categoryExists = await _productRepository.CategoryExistsAsync(dto.CategoryId);
            if (!categoryExists)
                throw new BadRequestException("Категорія не існує");

            var product = new Product
            {
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                IsAvailable = dto.IsAvailable,
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

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException("Товар не знайдено");

            var categoryExists = await _productRepository.CategoryExistsAsync(dto.CategoryId);
            if (!categoryExists)
                throw new BadRequestException("Категорія не існує");

            product.CategoryId = dto.CategoryId;
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.IsAvailable = dto.IsAvailable;
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
    }
}