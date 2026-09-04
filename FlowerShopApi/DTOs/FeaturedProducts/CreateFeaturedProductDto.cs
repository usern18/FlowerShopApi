using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.FeaturedProducts
{
    public class CreateFeaturedProductDto
    {
        [Required]
        public int ProductId { get; set; }
    }
}