using FlowerShopApi.Models;

namespace FlowerShopApi.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<List<Product>> GetOutOfStockAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task AddAsync(Product product);
        Task SaveChangesAsync();
        void Remove(Product product);
    }
}