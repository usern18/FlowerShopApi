using FlowerShopApi.Data;
using FlowerShopApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowerShopApi.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<bool> ExistsByNameAsync(string categoryName)
        {
            return await _context.Categories.AnyAsync(c => c.CategoryName == categoryName);
        }

        public async Task AddAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Remove(Category category)
        {
            _context.Categories.Remove(category);
        }
    }
}