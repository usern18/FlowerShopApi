using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowerShopApi.Models
{
    [Table("product_images")]
    public class ProductImage
    {
        [Key]
        [Column("image_id")]
        public int ImageId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [Column("image_url")]
        [MaxLength(255)]
        public string ImageUrl { get; set; } = string.Empty;

        [Column("is_main")]
        public bool IsMain { get; set; }

        public Product? Product { get; set; }
    }
}