
using FlowerShopApi.Data;
using FlowerShopApi.DTOs.FeaturedProducts;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowerShopApi.Services
{
    public class FeaturedProductService
    {
        private readonly AppDbContext _context;

        public FeaturedProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<FeaturedProduct>> GetAllAsync()
        {
            return await _context.FeaturedProducts
                .Include(fp => fp.Product)
                .ToListAsync();
        }

        public async Task<FeaturedProduct> CreateAsync(
            CreateFeaturedProductDto dto)
        {
            if (dto == null ||
                !dto.ProductId.HasValue ||
                dto.ProductId.Value <= 0)
            {
                throw new BadRequestException(
                    "Необхідно вказати коректний ProductId");
            }

            int productId = dto.ProductId.Value;

            var productExists = await _context.Products
                .AnyAsync(p => p.ProductId == productId);

            if (!productExists)
            {
                throw new BadRequestException("Товар не існує");
            }

            var featured = new FeaturedProduct
            {
                ProductId = productId
            };

            _context.FeaturedProducts.Add(featured);
            await _context.SaveChangesAsync();

            return featured;
        }

        public async Task<string> DeleteAsync(int id)
        {
            var featured = await _context.FeaturedProducts
                .FindAsync(id);

            if (featured == null)
            {
                throw new NotFoundException(
                    "Featured товар не знайдено");
            }

            _context.FeaturedProducts.Remove(featured);
            await _context.SaveChangesAsync();

            return "Featured товар видалено";
        }
    }
}
