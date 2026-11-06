
namespace FlowerShopApi.DTOs.Products
{
    public class ProductV2Dto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public bool InStock { get; set; }

        public int AvailableQuantity { get; set; }
    }
}
