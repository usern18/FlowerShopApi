using FlowerShopApi.Models;

namespace FlowerShopApi.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task UpdateAsync(User user);

    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneExistsAsync(string phone);
    Task AddAsync(User user);
    Task SaveChangesAsync();
    Task<User?> GetByEmailOrOAuthAsync(string email, string provider, string providerUserId);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
}