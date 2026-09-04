using FlowerShopApi.DTOs.Profile;

namespace FlowerShopApi.Services.Interfaces;

public interface IProfileService
{
    Task<ProfileResponseDto> GetProfileAsync(int userId);
    Task<ProfileResponseDto> UpdatePersonalInfoAsync(int userId, UpdatePersonalInfoDto dto);
    Task<ProfileResponseDto> UpdateDeliveryAddressAsync(int userId, UpdateDeliveryAddressDto dto);
    Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
}