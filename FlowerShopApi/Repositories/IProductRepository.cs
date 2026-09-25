using FlowerShopApi.Models;

namespace FlowerShopApi.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<List<Product>> GetByCategoryIdAsync(int categoryId);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task AddAsync(Product product);
        Task SaveChangesAsync();
        void Remove(Product product);
    }
}