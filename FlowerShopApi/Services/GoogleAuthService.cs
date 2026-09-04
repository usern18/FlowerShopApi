using FlowerShopApi.Exceptions;
using Google.Apis.Auth;

namespace FlowerShopApi.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(
            IConfiguration configuration,
            ILogger<GoogleAuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
        {
            var googleClientId = _configuration["GoogleAuth:ClientId"];

            if (string.IsNullOrWhiteSpace(googleClientId))
            {
                throw new InternalServerException("Google ClientId не налаштований");
            }

            try
            {
                return await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { googleClientId }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Недійсний Google idToken");
                throw new UnauthorizedException("Недійсний Google токен");
            }
        }
    }
}