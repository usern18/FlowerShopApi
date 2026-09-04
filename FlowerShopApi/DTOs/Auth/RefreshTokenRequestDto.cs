using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.Auth
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}