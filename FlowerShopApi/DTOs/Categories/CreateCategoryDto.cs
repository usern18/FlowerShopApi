using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.Categories
{
    public class CreateCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}