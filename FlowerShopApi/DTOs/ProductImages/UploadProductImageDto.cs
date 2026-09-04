using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FlowerShopApi.DTOs.ProductImages
{
    public class UploadProductImageDto
    {
        [Required]
        public IFormFile File { get; set; } = default!;

        public bool IsMain { get; set; }
    }
}