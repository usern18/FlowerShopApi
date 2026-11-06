
using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.FeaturedProducts
{
    public class CreateFeaturedProductDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int? ProductId { get; set; }
    }
}
