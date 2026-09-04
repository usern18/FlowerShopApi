using FlowerShopApi.DTOs.Auth;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;

namespace FlowerShopApi.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AuthService> _logger;
        private readonly Data.AppDbContext _context;

        public AuthService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IGoogleAuthService googleAuthService,
            ICurrentUserService currentUserService,
            ILogger<AuthService> logger,
            Data.AppDbContext context)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _googleAuthService = googleAuthService;
            _currentUserService = currentUserService;
            _logger = logger;
            _context = context;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            _logger.LogInformation("Спроба реєстрації користувача з email {Email}", dto.Email);

            await ValidateRegistrationAsync(dto);

            var customerRole = await GetCustomerRoleAsync();

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = customerRole.RoleId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Користувач {Email} успішно зареєстрований", dto.Email);

            return "Реєстрація успішна";
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation("Спроба входу користувача з email {Email}", dto.Email);

            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                _logger.LogWarning("Невдалий логін для {Email}: користувача не знайдено або пароль відсутній", dto.Email);
                throw new UnauthorizedException("Невірний email або пароль");
            }

            var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!passwordValid)
            {
                _logger.LogWarning("Невдалий логін для {Email}: неправильний пароль", dto.Email);
                throw new UnauthorizedException("Невірний email або пароль");
            }

            return await BuildAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto)
        {
            _logger.LogInformation("Спроба OAuth2 логіну через Google");

            var payload = await _googleAuthService.ValidateGoogleTokenAsync(dto.IdToken);

            var user = await _userRepository.GetByEmailOrOAuthAsync(
                payload.Email,
                "Google",
                payload.Subject
            );

            if (user == null)
            {
                user = await CreateGoogleUserAsync(payload);
                _logger.LogInformation("Створено нового користувача через Google OAuth2: {Email}", user.Email);
            }

            return await BuildAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            _logger.LogInformation("Спроба оновлення access token");

            var user = await _userRepository.GetByRefreshTokenAsync(dto.RefreshToken);

            if (user == null)
            {
                throw new UnauthorizedException("Недійсний або протермінований refresh token");
            }

            return await BuildAuthResponseAsync(user);
        }

        public async Task<string> LogoutAsync()
        {
            var userId = _currentUserService.GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("Користувача не знайдено");
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Користувач з id {UserId} виконав logout", userId);

            return "Вихід виконано успішно";
        }

        public async Task<object> MeAsync()
        {
            var userId = _currentUserService.GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("Користувача не знайдено");
            }

            return new
            {
                user.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Phone,
                Role = user.Role?.RoleName,
                user.OAuthProvider
            };
        }

        private async Task ValidateRegistrationAsync(RegisterDto dto)
        {
            if (await _userRepository.EmailExistsAsync(dto.Email))
            {
                throw new BadRequestException("Користувач з таким email вже існує");
            }

            if (await _userRepository.PhoneExistsAsync(dto.Phone))
            {
                throw new BadRequestException("Користувач з таким телефоном вже існує");
            }
        }

        private async Task<Role> GetCustomerRoleAsync()
        {
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "customer");

            if (customerRole == null)
            {
                throw new InternalServerException("Роль customer не знайдена в базі");
            }

            return customerRole;
        }

        private async Task<User> CreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload)
        {
            var customerRole = await GetCustomerRoleAsync();

            var user = new User
            {
                FirstName = payload.GivenName ?? "Google",
                LastName = payload.FamilyName ?? "User",
                Email = payload.Email,
                Phone = $"oauth_{Guid.NewGuid():N}".Substring(0, 20),
                PasswordHash = string.Empty,
                RoleId = customerRole.RoleId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                OAuthProvider = "Google",
                OAuthProviderUserId = payload.Subject
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return user;
        }

        private async Task<AuthResponseDto> BuildAuthResponseAsync(User user)
        {
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Токени видані для користувача {Email}", user.Email);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Email = user.Email,
                Role = user.Role?.RoleName ?? "customer"
            };
        }
    }
}