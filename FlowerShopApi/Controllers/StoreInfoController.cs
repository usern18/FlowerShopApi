using FlowerShopApi.DTOs.StoreInfo;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreInfoController : ControllerBase
    {
        private readonly StoreInfoService _storeInfoService;

        public StoreInfoController(StoreInfoService storeInfoService)
        {
            _storeInfoService = storeInfoService;
        }

        /// <summary>
        /// Отримує інформацію про магазин.
        /// </summary>
        /// <returns>
        /// Інформація про магазин: назва, адреса, телефон,
        /// email та опис.
        /// </returns>
        /// <response code="200">
        /// Інформацію про магазин успішно отримано.
        /// </response>
        /// <response code="404">
        /// Інформацію про магазин не знайдено.
        /// </response>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var info = await _storeInfoService.GetAsync();
            return Ok(info);
        }

        /// <summary>
        /// Оновлює інформацію про магазин.
        /// </summary>
        /// <param name="dto">
        /// Нові дані магазину: назва, адреса, телефон,
        /// email та опис.
        /// </param>
        /// <returns>
        /// Повідомлення про успішне оновлення інформації.
        /// </returns>
        /// <response code="200">
        /// Інформацію про магазин успішно оновлено.
        /// </response>
        /// <response code="400">
        /// Передані некоректні або неповні дані.
        /// </response>
        /// <response code="401">
        /// Користувач не авторизований.
        /// </response>
        /// <response code="403">
        /// Користувач не має прав адміністратора.
        /// </response>
        /// <response code="404">
        /// Інформацію про магазин не знайдено.
        /// </response>
        [Authorize(Roles = "admin")]
        [HttpPut]
        public async Task<IActionResult> Update(UpdateStoreInfoDto dto)
        {
            var message = await _storeInfoService.UpdateAsync(dto);
            return Ok(new { message });
        }
    }
}