using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowerShopApi.Models
{
    [Table("order_items")]
    public class OrderItem
    {
        [Key]
        [Column("order_item_id")]
        public int OrderItemId { get; set; }

        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("price_at_moment", TypeName = "decimal(10,2)")]
        public decimal PriceAtMoment { get; set; }

        [Column("discount_at_moment", TypeName = "decimal(5,2)")]
        public decimal DiscountAtMoment { get; set; }

        [Column("subtotal", TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}