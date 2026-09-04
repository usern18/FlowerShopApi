using FlowerShopApi.DTOs.Profile;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services.Interfaces;

namespace FlowerShopApi.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(IUserRepository userRepository, ILogger<ProfileService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ProfileResponseDto> GetProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) throw new NotFoundException("Користувача не знайдено");
        _logger.LogInformation("Отримання профілю користувача {UserId}", userId);
        return MapToDto(user);
    }

    public async Task<ProfileResponseDto> UpdatePersonalInfoAsync(int userId, UpdatePersonalInfoDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) throw new NotFoundException("Користувача не знайдено");

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Оновлено особисті дані користувача {UserId}", userId);
        return MapToDto(user);
    }

    public async Task<ProfileResponseDto> UpdateDeliveryAddressAsync(int userId, UpdateDeliveryAddressDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) throw new NotFoundException("Користувача не знайдено");

        user.Region = string.IsNullOrWhiteSpace(dto.Region) ? null : dto.Region.Trim();
        user.City = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City.Trim();
        user.StreetAddress = string.IsNullOrWhiteSpace(dto.StreetAddress) ? null : dto.StreetAddress.Trim();
        user.PostalCode = string.IsNullOrWhiteSpace(dto.PostalCode) ? null : dto.PostalCode.Trim();

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Оновлено адресу доставки користувача {UserId}", userId);
        return MapToDto(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null) throw new NotFoundException("Користувача не знайдено");
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            throw new BadRequestException("Для цього акаунта зміна пароля недоступна");

        var isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid)
            throw new BadRequestException("Поточний пароль невірний");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Користувач {UserId} змінив пароль", userId);
    }

    private static ProfileResponseDto MapToDto(FlowerShopApi.Models.User user)
    {
        return new ProfileResponseDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MiddleName = user.MiddleName,
            Email = user.Email,
            Phone = user.Phone,
            Region = user.Region,
            City = user.City,
            StreetAddress = user.StreetAddress,
            PostalCode = user.PostalCode,
            Role = user.Role?.RoleName
        };
    }
}