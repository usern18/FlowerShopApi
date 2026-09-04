using FlowerShopApi.Data;
using FlowerShopApi.DTOs.StoreInfo;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowerShopApi.Services
{
    public class StoreInfoService
    {
        private readonly AppDbContext _context;

        public StoreInfoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StoreInfo> GetAsync()
        {
            var info = await _context.StoreInfos.FirstOrDefaultAsync();
            if (info == null)
                throw new NotFoundException("Інформацію про магазин не знайдено");

            return info;
        }

        public async Task<string> UpdateAsync(UpdateStoreInfoDto dto)
        {
            var info = await _context.StoreInfos.FirstOrDefaultAsync();
            if (info == null)
                throw new NotFoundException("Інформацію про магазин не знайдено");

            info.StoreName = dto.StoreName;
            info.Address = dto.Address;
            info.Phone = dto.Phone;
            info.Email = dto.Email;
            info.Description = dto.Description;

            await _context.SaveChangesAsync();
            return "Інформацію про магазин оновлено";
        }
    }
}