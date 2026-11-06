using FlowerShopApi.DTOs.Discounts;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowerShopApi.Models;
using FlowerShopApi.DTOs.Common;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiscountsController : ControllerBase
    {
        private readonly DiscountService _discountService;

        public DiscountsController(DiscountService discountService)
        {
            _discountService = discountService;
        }

        /// <summary>
        /// Отримує список усіх знижок на товари.
        /// </summary>
        /// <returns>Список знижок разом із пов'язаними товарами.</returns>
        /// <response code="200">Список знижок успішно отримано.</response>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var discounts = await _discountService.GetAllAsync();
            return Ok(discounts);
        }

        /// <summary>
        /// Створює нову знижку для товару.
        /// </summary>
        /// <param name="dto">
        /// Ідентифікатор товару, відсоток знижки та період її дії.
        /// </param>
        /// <returns>Створена знижка.</returns>
        /// <response code="200">Знижку успішно створено.</response>
        /// <response code="400">Товар із вказаним ідентифікатором не існує.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateDiscountDto dto)
        {
            var discount = await _discountService.CreateAsync(dto);
            return Ok(discount);
        }

        /// <summary>
        /// Видаляє знижку.
        /// </summary>
        /// <param name="id">Ідентифікатор знижки.</param>
        /// <returns>Повідомлення про результат видалення.</returns>
        /// <response code="200">Знижку успішно видалено.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        /// <response code="404">Знижку не знайдено.</response>
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _discountService.DeleteAsync(id);
            return Ok(new { message });
        }
    }
}