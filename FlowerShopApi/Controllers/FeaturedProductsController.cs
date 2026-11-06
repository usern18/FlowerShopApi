using FlowerShopApi.DTOs.FeaturedProducts;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeaturedProductsController : ControllerBase
    {
        private readonly FeaturedProductService _featuredProductService;

        public FeaturedProductsController(FeaturedProductService featuredProductService)
        {
            _featuredProductService = featuredProductService;
        }

        /// <summary>
        /// Отримує список рекомендованих товарів.
        /// </summary>
        /// <returns>
        /// Список товарів, позначених як рекомендовані.
        /// </returns>
        /// <response code="200">
        /// Список рекомендованих товарів успішно отримано.
        /// </response>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var featured = await _featuredProductService.GetAllAsync();
            return Ok(featured);
        }

        /// <summary>
        /// Додає товар до списку рекомендованих товарів.
        /// </summary>
        /// <param name="dto">
        /// Дані товару, який необхідно додати до списку рекомендованих.
        /// </param>
        /// <returns>
        /// Створений запис рекомендованого товару.
        /// </returns>
        /// <response code="200">
        /// Товар успішно додано до списку рекомендованих.
        /// </response>
        /// <response code="400">
        /// Товар із вказаним ідентифікатором не існує
        /// або передані некоректні дані.
        /// </response>
        /// <response code="401">
        /// Користувач не авторизований.
        /// </response>
        /// <response code="403">
        /// Користувач не має прав адміністратора.
        /// </response>
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateFeaturedProductDto dto)
        {
            var featured = await _featuredProductService.CreateAsync(dto);
            return Ok(featured);
        }

        /// <summary>
        /// Видаляє товар зі списку рекомендованих товарів.
        /// </summary>
        /// <param name="id">
        /// Ідентифікатор запису рекомендованого товару.
        /// </param>
        /// <returns>
        /// Повідомлення про результат видалення.
        /// </returns>
        /// <response code="200">
        /// Товар успішно видалено зі списку рекомендованих.
        /// </response>
        /// <response code="401">
        /// Користувач не авторизований.
        /// </response>
        /// <response code="403">
        /// Користувач не має прав адміністратора.
        /// </response>
        /// <response code="404">
        /// Рекомендований товар із таким ідентифікатором не знайдено.
        /// </response>
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _featuredProductService.DeleteAsync(id);
            return Ok(new { message });
        }
    }
}