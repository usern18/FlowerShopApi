using FlowerShopApi.Models;

namespace FlowerShopApi.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
    }
}