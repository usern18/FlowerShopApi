using FlowerShopApi.Data;
using FlowerShopApi.DTOs.Discounts;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowerShopApi.Services
{
    public class DiscountService
    {
        private readonly AppDbContext _context;

        public DiscountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Discount>> GetAllAsync()
        {
            return await _context.Discounts.Include(d => d.Product).ToListAsync();
        }

        public async Task<Discount> CreateAsync(CreateDiscountDto dto)
        {
            var productExists = await _context.Products.AnyAsync(p => p.ProductId == dto.ProductId);
            if (!productExists)
                throw new BadRequestException("Товар не існує");

            var discount = new Discount
            {
                ProductId = dto.ProductId,
                DiscountPercent = dto.DiscountPercent,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            return discount;
        }

        public async Task<string> DeleteAsync(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
                throw new NotFoundException("Знижку не знайдено");

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();

            return "Знижку видалено";
        }
    }
}