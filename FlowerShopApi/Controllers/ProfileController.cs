using System.Security.Claims;
using FlowerShopApi.DTOs.Profile;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowerShopApi.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _profileService.GetProfileAsync(userId);
        return Ok(result);
    }

    [HttpPut("personal-info")]
    public async Task<IActionResult> UpdatePersonalInfo([FromBody] UpdatePersonalInfoDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _profileService.UpdatePersonalInfoAsync(userId, dto);
        return Ok(result);
    }

    [HttpPut("delivery-address")]
    public async Task<IActionResult> UpdateDeliveryAddress([FromBody] UpdateDeliveryAddressDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _profileService.UpdateDeliveryAddressAsync(userId, dto);
        return Ok(result);
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        await _profileService.ChangePasswordAsync(userId, dto);
        return Ok(new { message = "Пароль успішно змінено" });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("Недійсний токен користувача");
        return userId;
    }
}