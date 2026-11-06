
using FlowerShopApi.DTOs.Products;
using FlowerShopApi.Models;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api/v2/products")]
    public class ProductsV2Controller : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsV2Controller(ProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Отримує всі товари у форматі API v2.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();

            var result = products.Select(ToV2Dto).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Отримує товар за ID у форматі API v2.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);

            return Ok(ToV2Dto(product));
        }

        private static ProductV2Dto ToV2Dto(Product product)
        {
            return new ProductV2Dto
            {
                Id = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                InStock = product.StockQuantity > 0
                    && product.IsAvailable,
                AvailableQuantity = product.StockQuantity
            };
        }
    }
}
