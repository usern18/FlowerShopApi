using FlowerShopApi.Models;

namespace FlowerShopApi.Repositories
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllAsync();
        Task<List<Order>> GetByUserIdAsync(int userId);
        Task<Order?> GetByIdAsync(int id);
        Task<Product?> GetProductByIdAsync(int productId);
        Task AddAsync(Order order);
        Task SaveChangesAsync();
    }
}