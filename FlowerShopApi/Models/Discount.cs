using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowerShopApi.Models
{
    [Table("discounts")]
    public class Discount
    {
        [Key]
        [Column("discount_id")]
        public int DiscountId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("discount_percent", TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        public Product? Product { get; set; }
    }
}