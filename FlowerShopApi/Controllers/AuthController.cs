using FlowerShopApi.DTOs.Auth;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowerShopApi.DTOs.Common;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Реєструє нового користувача в системі.
        /// </summary>
        /// <param name="dto">
        /// Дані для реєстрації користувача: ім'я, прізвище, email,
        /// номер телефону та пароль.
        /// </param>
        /// <returns>Повідомлення про результат реєстрації.</returns>
        /// <response code="200">Користувача успішно зареєстровано.</response>
        /// <response code="400">
        /// Користувач із таким email або номером телефону вже існує
        /// або передані некоректні дані.
        /// </response>
        /// <response code="500">
        /// Внутрішня помилка сервера, наприклад необхідна роль відсутня в базі даних.
        /// </response>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var message = await _authService.RegisterAsync(dto);
            return Ok(new { message });
        }

        /// <summary>
        /// Авторизує користувача за email та паролем.
        /// </summary>
        /// <param name="dto">Email та пароль користувача.</param>
        /// <returns>
        /// Access token, refresh token, email та роль авторизованого користувача.
        /// </returns>
        /// <response code="200">Авторизація виконана успішно.</response>
        /// <response code="401">Невірний email або пароль.</response>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var response = await _authService.LoginAsync(dto);
            return Ok(response);
        }

        /// <summary>
        /// Авторизує або реєструє користувача за допомогою Google OAuth2.
        /// </summary>
        /// <param name="dto">Google ID token, отриманий після авторизації через Google.</param>
        /// <returns>
        /// Access token, refresh token, email та роль користувача.
        /// </returns>
        /// <response code="200">Авторизація через Google виконана успішно.</response>
        /// <response code="400">Передано некоректні дані запиту.</response>
        /// <response code="401">Google token недійсний.</response>
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginDto dto)
        {
            var response = await _authService.GoogleLoginAsync(dto);
            return Ok(response);
        }

        /// <summary>
        /// Оновлює access token за допомогою refresh token.
        /// </summary>
        /// <param name="dto">Refresh token користувача.</param>
        /// <returns>
        /// Нові access token та refresh token разом із даними користувача.
        /// </returns>
        /// <response code="200">Токени успішно оновлено.</response>
        /// <response code="401">Refresh token недійсний або протермінований.</response>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto dto)
        {
            var response = await _authService.RefreshTokenAsync(dto);
            return Ok(response);
        }

        /// <summary>
        /// Завершує поточну сесію користувача.
        /// </summary>
        /// <returns>Повідомлення про успішний вихід із системи.</returns>
        /// <response code="200">Вихід виконано успішно.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="404">Користувача не знайдено.</response>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var message = await _authService.LogoutAsync();
            return Ok(new { message });
        }

        /// <summary>
        /// Отримує інформацію про поточного авторизованого користувача.
        /// </summary>
        /// <returns>
        /// Ідентифікатор, ім'я, прізвище, email, телефон,
        /// роль та OAuth-провайдер користувача.
        /// </returns>
        /// <response code="200">Дані користувача успішно отримано.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="404">Користувача не знайдено.</response>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _authService.MeAsync();
            return Ok(user);
        }
    }
}