
using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.Products
{
    public class CreateProductDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int? CategoryId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, 999999)]
        public decimal? Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        [Required]
        public bool? IsAvailable { get; set; }
    }
}
