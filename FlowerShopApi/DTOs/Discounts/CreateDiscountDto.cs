using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.Discounts
{
    public class CreateDiscountDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }
    }
}