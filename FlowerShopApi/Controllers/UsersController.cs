using FlowerShopApi.DTOs.Users;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowerShopApi.Models;
using FlowerShopApi.DTOs.Common;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Отримує список усіх зареєстрованих користувачів.
        /// </summary>
        /// <returns>Список користувачів разом із їхніми ролями.</returns>
        /// <response code="200">Список користувачів успішно отримано.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        /// <summary>
        /// Отримує користувача за його ідентифікатором.
        /// </summary>
        /// <param name="id">Ідентифікатор користувача.</param>
        /// <returns>Дані користувача.</returns>
        /// <response code="200">Користувача успішно знайдено.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        /// <response code="404">Користувача не знайдено.</response>
        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(user);
        }

        /// <summary>
        /// Отримує профіль поточного авторизованого користувача.
        /// </summary>
        /// <returns>Дані профілю користувача.</returns>
        /// <response code="200">Профіль успішно отримано.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="404">Користувача не знайдено.</response>
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var profile = await _userService.GetProfileAsync();
            return Ok(profile);
        }

        /// <summary>
        /// Оновлює основні дані профілю поточного користувача.
        /// </summary>
        /// <param name="dto">Нове ім'я, прізвище та номер телефону.</param>
        /// <returns>Повідомлення про результат оновлення.</returns>
        /// <response code="200">Профіль успішно оновлено.</response>
        /// <response code="400">Передані некоректні дані.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="404">Користувача не знайдено.</response>
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var message = await _userService.UpdateProfileAsync(dto);
            return Ok(new { message });
        }

        /// <summary>
        /// Змінює роль користувача.
        /// </summary>
        /// <param name="id">Ідентифікатор користувача.</param>
        /// <param name="dto">Ідентифікатор нової ролі.</param>
        /// <returns>Повідомлення про результат зміни ролі.</returns>
        /// <response code="200">Роль користувача успішно змінено.</response>
        /// <response code="400">Вказана роль не існує.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        /// <response code="404">Користувача не знайдено.</response>
        [Authorize(Roles = "admin")]
        [HttpPut("{id}/role")]
        public async Task<IActionResult> ChangeRole(int id, ChangeRoleDto dto)
        {
            var message = await _userService.ChangeRoleAsync(id, dto);
            return Ok(new { message });
        }
    }
}