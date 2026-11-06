using System.Security.Claims;
using FlowerShopApi.Exceptions;

namespace FlowerShopApi.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            {
                throw new UnauthorizedException(
                    "Некоректний або відсутній ідентифікатор користувача");
            }

            return userId;
        }
    }
}