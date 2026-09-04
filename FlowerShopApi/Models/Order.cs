using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowerShopApi.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; }

        [Required]
        [Column("status")]
        public string Status { get; set; } = "new";

        [Column("total_amount", TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column("delivery_address")]
        [MaxLength(255)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required]
        [Column("recipient_name")]
        [MaxLength(150)]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        [Column("recipient_phone")]
        [MaxLength(20)]
        public string RecipientPhone { get; set; } = string.Empty;

        [Column("comment")]
        public string? Comment { get; set; }

        public User? User { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}