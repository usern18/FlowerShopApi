using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowerShopApi.Models
{
    [Table("featured_products")]
    public class FeaturedProduct
    {
        [Key]
        [Column("featured_id")]
        public int FeaturedId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        public Product? Product { get; set; }
    }
}