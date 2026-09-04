using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.Orders
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}