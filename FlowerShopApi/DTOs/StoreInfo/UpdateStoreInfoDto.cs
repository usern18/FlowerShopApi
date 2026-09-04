using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.StoreInfo
{
    public class UpdateStoreInfoDto
    {
        [Required]
        public string StoreName { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}