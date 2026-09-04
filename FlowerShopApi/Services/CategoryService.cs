using FlowerShopApi.DTOs.Categories;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;

namespace FlowerShopApi.Services
{
    public class CategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            _logger.LogInformation("Отримання всіх категорій");
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<Category> GetByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new NotFoundException("Категорію не знайдено");

            return category;
        }

        public async Task<Category> CreateAsync(CreateCategoryDto dto)
        {
            if (await _categoryRepository.ExistsByNameAsync(dto.CategoryName))
                throw new BadRequestException("Категорія з такою назвою вже існує");

            var category = new Category
            {
                CategoryName = dto.CategoryName,
                Description = dto.Description
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            _logger.LogInformation("Створено категорію {CategoryName}", dto.CategoryName);

            return category;
        }

        public async Task<string> UpdateAsync(int id, CreateCategoryDto dto)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new NotFoundException("Категорію не знайдено");

            category.CategoryName = dto.CategoryName;
            category.Description = dto.Description;

            await _categoryRepository.SaveChangesAsync();

            return "Категорію оновлено";
        }

        public async Task<string> DeleteAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new NotFoundException("Категорію не знайдено");

            _categoryRepository.Remove(category);
            await _categoryRepository.SaveChangesAsync();

            return "Категорію видалено";
        }
    }
}