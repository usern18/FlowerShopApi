using System.Security.Claims;
using FlowerShopApi.DTOs.Profile;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowerShopApi.DTOs.Common;

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

    /// <summary>
    /// Отримує повний профіль поточного користувача.
    /// </summary>
    /// <returns>
    /// Персональні дані користувача, контактна інформація,
    /// адреса доставки та роль.
    /// </returns>
    /// <response code="200">Профіль успішно отримано.</response>
    /// <response code="401">Користувач не авторизований або token недійсний.</response>
    /// <response code="404">Користувача не знайдено.</response>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _profileService.GetProfileAsync(userId);
        return Ok(result);
    }
    /// <summary>
    /// Оновлює персональну інформацію поточного користувача.
    /// </summary>
    /// <param name="dto">
    /// Ім'я, прізвище, по батькові та номер телефону користувача.
    /// </param>
    /// <returns>Оновлений профіль користувача.</returns>
    /// <response code="200">Персональні дані успішно оновлено.</response>
    /// <response code="400">Передані некоректні дані.</response>
    /// <response code="401">Користувач не авторизований.</response>
    /// <response code="404">Користувача не знайдено.</response>
    [HttpPut("personal-info")]
    public async Task<IActionResult> UpdatePersonalInfo([FromBody] UpdatePersonalInfoDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _profileService.UpdatePersonalInfoAsync(userId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Оновлює адресу доставки поточного користувача.
    /// </summary>
    /// <param name="dto">
    /// Область, місто, вулиця та поштовий індекс користувача.
    /// </param>
    /// <returns>Оновлений профіль користувача.</returns>
    /// <response code="200">Адресу доставки успішно оновлено.</response>
    /// <response code="400">Передані некоректні дані.</response>
    /// <response code="401">Користувач не авторизований.</response>
    /// <response code="404">Користувача не знайдено.</response>
    [HttpPut("delivery-address")]
    public async Task<IActionResult> UpdateDeliveryAddress([FromBody] UpdateDeliveryAddressDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _profileService.UpdateDeliveryAddressAsync(userId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Змінює пароль поточного користувача.
    /// </summary>
    /// <param name="dto">
    /// Поточний пароль користувача та новий пароль.
    /// </param>
    /// <returns>Повідомлення про успішну зміну пароля.</returns>
    /// <response code="200">Пароль успішно змінено.</response>
    /// <response code="400">
    /// Поточний пароль неправильний або зміна пароля недоступна для цього акаунта.
    /// </response>
    /// <response code="401">Користувач не авторизований.</response>
    /// <response code="404">Користувача не знайдено.</response>
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