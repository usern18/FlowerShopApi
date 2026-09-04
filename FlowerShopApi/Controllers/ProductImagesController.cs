using FlowerShopApi.DTOs.ProductImages;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("products/{productId}/images")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var images = await _productImageService.GetByProductIdAsync(productId);
            return Ok(images);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("products/{productId}/images")]
        public async Task<IActionResult> Upload(int productId, [FromForm] UploadProductImageDto dto)
        {
            var image = await _productImageService.UploadAsync(productId, dto);
            return Ok(image);
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("product-images/{imageId}")]
        public async Task<IActionResult> Delete(int imageId)
        {
            var message = await _productImageService.DeleteAsync(imageId);
            return Ok(new { message });
        }
    }
}