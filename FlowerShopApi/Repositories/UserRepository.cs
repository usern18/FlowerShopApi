using FlowerShopApi.Data;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlowerShopApi.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> EmailExistsAsync(string email) => await _context.Users.AnyAsync(u => u.Email == email);
    public async Task<bool> PhoneExistsAsync(string phone) => await _context.Users.AnyAsync(u => u.Phone == phone);

    public async Task AddAsync(User user) => await _context.Users.AddAsync(user);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task<User?> GetByEmailOrOAuthAsync(string email, string provider, string providerUserId)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.Email == email ||
                (u.OAuthProvider == provider && u.OAuthProviderUserId == providerUserId));
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.RefreshToken == refreshToken &&
                u.RefreshTokenExpiryTime != null &&
                u.RefreshTokenExpiryTime > DateTime.UtcNow);
    }
}