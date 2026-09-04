using System.Security.Claims;
using FlowerShopApi.Data;
using FlowerShopApi.DTOs.Users;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowerShopApi.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedException("Користувач не авторизований");

            return int.Parse(userIdClaim);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users.Include(u => u.Role).ToListAsync();
        }

        public async Task<User> GetByIdAsync(int id)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
                throw new NotFoundException("Користувача не знайдено");

            return user;
        }

        public async Task<User> GetProfileAsync()
        {
            var userId = GetCurrentUserId();
            return await GetByIdAsync(userId);
        }

        public async Task<string> UpdateProfileAsync(UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new NotFoundException("Користувача не знайдено");

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Phone = dto.Phone;

            await _context.SaveChangesAsync();
            return "Профіль оновлено";
        }

        public async Task<string> ChangeRoleAsync(int userId, ChangeRoleDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new NotFoundException("Користувача не знайдено");

            var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId);
            if (!roleExists)
                throw new BadRequestException("Роль не існує");

            user.RoleId = dto.RoleId;
            await _context.SaveChangesAsync();

            return "Роль користувача оновлено";
        }
    }
}