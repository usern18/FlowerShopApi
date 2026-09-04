using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowerShopApi.Models
{
    [Table("store_info")]
    public class StoreInfo
    {
        [Key]
        [Column("store_info_id")]
        public int StoreInfoId { get; set; }

        [Column("store_name")]
        [MaxLength(150)]
        public string StoreName { get; set; } = string.Empty;

        [Column("address")]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Column("phone")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }
    }
}