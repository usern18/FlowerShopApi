using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.Orders
{
    public class CreateOrderDto
    {
        [Required]
        [MaxLength(255)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string RecipientPhone { get; set; } = string.Empty;

        public string? Comment { get; set; }

        [Required]
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }
}