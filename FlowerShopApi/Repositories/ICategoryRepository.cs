using FlowerShopApi.Models;

namespace FlowerShopApi.Repositories
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string categoryName);
        Task AddAsync(Category category);
        Task SaveChangesAsync();
        void Remove(Category category);
    }
}