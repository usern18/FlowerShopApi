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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var featured = await _featuredProductService.GetAllAsync();
            return Ok(featured);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateFeaturedProductDto dto)
        {
            var featured = await _featuredProductService.CreateAsync(dto);
            return Ok(featured);
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _featuredProductService.DeleteAsync(id);
            return Ok(new { message });
        }
    }
}