using FlowerShopApi.DTOs.Categories;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowerShopApi.Models;
using FlowerShopApi.DTOs.Common;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoriesController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Отримує список усіх категорій товарів.
        /// </summary>
        /// <returns>Список категорій.</returns>
        /// <response code="200">Категорії успішно отримано.</response>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        /// <summary>
        /// Отримує категорію за її ідентифікатором.
        /// </summary>
        /// <param name="id">Ідентифікатор категорії.</param>
        /// <returns>Дані категорії.</returns>
        /// <response code="200">Категорію успішно знайдено.</response>
        /// <response code="404">Категорію не знайдено.</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            return Ok(category);
        }

        /// <summary>
        /// Створює нову категорію товарів.
        /// </summary>
        /// <param name="dto">Назва та опис нової категорії.</param>
        /// <returns>Створена категорія.</returns>
        /// <response code="201">Категорію успішно створено.</response>
        /// <response code="400">Категорія з такою назвою вже існує.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateCategoryDto dto)
        {
            var category = await _categoryService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, category);
        }

        /// <summary>
        /// Оновлює існуючу категорію.
        /// </summary>
        /// <param name="id">Ідентифікатор категорії.</param>
        /// <param name="dto">Нові дані категорії.</param>
        /// <returns>Повідомлення про результат оновлення.</returns>
        /// <response code="200">Категорію успішно оновлено.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        /// <response code="404">Категорію не знайдено.</response>
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CreateCategoryDto dto)
        {
            var message = await _categoryService.UpdateAsync(id, dto);
            return Ok(new { message });
        }

        /// <summary>
        /// Видаляє категорію товарів.
        /// </summary>
        /// <param name="id">Ідентифікатор категорії.</param>
        /// <returns>Повідомлення про результат видалення.</returns>
        /// <response code="200">Категорію успішно видалено.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        /// <response code="404">Категорію не знайдено.</response>
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _categoryService.DeleteAsync(id);
            return Ok(new { message });
        }
    }
}