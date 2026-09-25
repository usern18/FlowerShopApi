using FlowerShopApi.DTOs.Products;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowerShopApi.Models;
using FlowerShopApi.DTOs.Common;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }


        /// <summary>
        /// Отримує список усіх товарів магазину.
        /// </summary>
        /// <returns>Список товарів.</returns>
        /// <response code="200">Список товарів успішно отримано.</response>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }


        [HttpGet("out-of-stock")]
        public async Task<IActionResult> GetOutOfStock()
        {
            var products = await _productService.GetOutOfStockAsync();
            return Ok(products);
        }
        /// <summary>
        /// Отримує товар за його унікальним ідентифікатором.
        /// </summary>
        /// <param name="id">Ідентифікатор товару.</param>
        /// <returns>Дані знайденого товару.</returns>
        /// <response code="200">Товар успішно знайдено.</response>
        /// <response code="404">Товар із таким ідентифікатором не знайдено.</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            return Ok(product);
        }

        /// <summary>
        /// Створює новий товар у каталозі магазину.
        /// </summary>
        /// <param name="dto">
        /// Дані нового товару: категорія, назва, опис, ціна,
        /// кількість на складі та доступність.
        /// </param>
        /// <returns>Створений товар.</returns>
        /// <response code="201">Товар успішно створено.</response>
        /// <response code="400">
        /// Передані некоректні дані або вказана категорія не існує.
        /// </response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            var product = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, product);
        }

        /// <summary>
        /// Оновлює дані існуючого товару.
        /// </summary>
        /// <param name="id">Ідентифікатор товару.</param>
        /// <param name="dto">Нові дані товару.</param>
        /// <returns>Повідомлення про результат оновлення.</returns>
        /// <response code="200">Товар успішно оновлено.</response>
        /// <response code="400">Вказана категорія не існує або дані некоректні.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        /// <response code="404">Товар не знайдено.</response>
        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CreateProductDto dto)
        {
            var message = await _productService.UpdateAsync(id, dto);
            return Ok(new { message });
        }

        /// <summary>
        /// Видаляє товар із каталогу.
        /// </summary>
        /// <param name="id">Ідентифікатор товару.</param>
        /// <returns>Повідомлення про результат видалення.</returns>
        /// <response code="200">Товар успішно видалено.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        /// <response code="404">Товар не знайдено.</response>
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _productService.DeleteAsync(id);
            return Ok(new { message });
        }
    }
}