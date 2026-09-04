using FlowerShopApi.Models;

namespace FlowerShopApi.Repositories
{
    public interface IProductImageRepository
    {
        Task<bool> ProductExistsAsync(int productId);
        Task<List<ProductImage>> GetByProductIdAsync(int productId);
        Task<ProductImage?> GetByIdAsync(int imageId);
        Task AddAsync(ProductImage image);
        Task SaveChangesAsync();
        void Remove(ProductImage image);
    }
}