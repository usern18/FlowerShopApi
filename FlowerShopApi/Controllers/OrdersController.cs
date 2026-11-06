using FlowerShopApi.DTOs.Orders;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowerShopApi.Models;
using FlowerShopApi.DTOs.Common;

namespace FlowerShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Створює нове замовлення для поточного користувача.
        /// </summary>
        /// <param name="dto">
        /// Адреса доставки, дані отримувача, коментар та список товарів із кількістю.
        /// </param>
        /// <returns>Створене замовлення.</returns>
        /// <response code="201">Замовлення успішно створено.</response>
        /// <response code="400">
        /// Замовлення порожнє або на складі недостатньо необхідного товару.
        /// </response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="404">Один із товарів замовлення не знайдено.</response>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderDto dto)
        {
            var order = await _orderService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, order);
        }

        /// <summary>
        /// Отримує всі замовлення поточного авторизованого користувача.
        /// </summary>
        /// <returns>Список замовлень користувача.</returns>
        /// <response code="200">Замовлення успішно отримано.</response>
        /// <response code="401">Користувач не авторизований.</response>
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var orders = await _orderService.GetMyOrdersAsync();
            return Ok(orders);
        }

        /// <summary>
        /// Отримує замовлення за його ідентифікатором.
        /// </summary>
        /// <param name="id">Ідентифікатор замовлення.</param>
        /// <returns>Інформація про замовлення.</returns>
        /// <response code="200">Замовлення успішно знайдено.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="404">Замовлення не знайдено.</response>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _orderService.GetByIdAsync(id);
            return Ok(order);
        }

        /// <summary>
        /// Отримує список усіх замовлень магазину.
        /// </summary>
        /// <returns>Список усіх замовлень.</returns>
        /// <response code="200">Замовлення успішно отримано.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _orderService.GetAllAsync();
            return Ok(orders);
        }

        /// <summary>
        /// Змінює статус замовлення.
        /// </summary>
        /// <param name="id">Ідентифікатор замовлення.</param>
        /// <param name="dto">Новий статус замовлення.</param>
        /// <returns>Повідомлення про результат зміни статусу.</returns>
        /// <response code="200">Статус замовлення успішно оновлено.</response>
        /// <response code="401">Користувач не авторизований.</response>
        /// <response code="403">Користувач не має прав адміністратора.</response>
        /// <response code="404">Замовлення не знайдено.</response>
        [Authorize(Roles = "admin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDto dto)
        {
            var message = await _orderService.UpdateStatusAsync(id, dto);
            return Ok(new { message });
        }
    }
}