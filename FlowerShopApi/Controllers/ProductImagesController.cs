using FlowerShopApi.DTOs.ProductImages;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowerShopApi.Models;
using FlowerShopApi.DTOs.Common;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class ProductImagesController : ControllerBase
    {
        private readonly ProductImageService _productImageService;

        public ProductImagesController(ProductImageService productImageService)
        {
            _productImageService = productImageService;
        }

        /// <summary>
        /// Отримує всі зображення певного товару.
        /// </summary>
        /// <param name="productId">Ідентифікатор товару.</param>
        /// <returns>Список зображень товару.</returns>
        /// <response code="200">Зображення успішно отримано.</response>
        /// <response code="404">Товар не знайдено.</response>
        [HttpGet("products/{productId}/images")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var images = await _productImageService.GetByProductIdAsync(productId);
            return Ok(images);
        }

        /// <summary>
        /// Завантажує нове зображення для товару.
        /// </summary>
        /// <param name="productId">Ідентифікатор товару.</param>
        /// <param name="dto">
        /// Файл зображення та ознака того, чи є воно головним зображенням товару.
        /// </param>
        /// <returns>Інформація про завантажене зображення.</returns>
        /// <response code="200">Зображення успішно завантажено.</response>
        /// <response code="400">
        /// Файл не вибрано або використано непідтримуваний формат.
        /// Дозволені JPG, JPEG, PNG та WEBP.
        /// </response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        /// <response code="404">Товар не знайдено.</response>
        [Authorize(Roles = "admin")]
        [HttpPost("products/{productId}/images")]
        public async Task<IActionResult> Upload(int productId, [FromForm] UploadProductImageDto dto)
        {
            var image = await _productImageService.UploadAsync(productId, dto);
            return Ok(image);
        }

        /// <summary>
        /// Видаляє зображення товару.
        /// </summary>
        /// <param name="imageId">Ідентифікатор зображення.</param>
        /// <returns>Повідомлення про результат видалення.</returns>
        /// <response code="200">Зображення успішно видалено.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        /// <response code="404">Зображення не знайдено.</response>
        [Authorize(Roles = "admin")]
        [HttpDelete("product-images/{imageId}")]
        public async Task<IActionResult> Delete(int imageId)
        {
            var message = await _productImageService.DeleteAsync(imageId);
            return Ok(new { message });
        }
    }
}