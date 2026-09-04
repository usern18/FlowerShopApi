using System.ComponentModel.DataAnnotations;

namespace FlowerShopApi.DTOs.Auth
{
    public class GoogleLoginDto
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }
}