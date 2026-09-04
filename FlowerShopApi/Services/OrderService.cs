using System.Security.Claims;
using FlowerShopApi.DTOs.Orders;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;

namespace FlowerShopApi.Services
{
    public class OrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim))
                throw new UnauthorizedException("Користувач не авторизований");

            return int.Parse(claim);
        }

        public async Task<Order> CreateAsync(CreateOrderDto dto)
        {
            var userId = GetCurrentUserId();

            if (dto.Items.Count == 0)
                throw new BadRequestException("Замовлення не може бути порожнім");

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "new",
                DeliveryAddress = dto.DeliveryAddress,
                RecipientName = dto.RecipientName,
                RecipientPhone = dto.RecipientPhone,
                Comment = dto.Comment
            };

            decimal total = 0;

            foreach (var itemDto in dto.Items)
            {
                var product = await _orderRepository.GetProductByIdAsync(itemDto.ProductId);
                if (product == null)
                    throw new NotFoundException($"Товар з id {itemDto.ProductId} не знайдено");

                if (product.StockQuantity < itemDto.Quantity)
                    throw new BadRequestException($"Недостатня кількість товару: {product.Name}");

                var subtotal = product.Price * itemDto.Quantity;
                total += subtotal;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.ProductId,
                    Quantity = itemDto.Quantity,
                    PriceAtMoment = product.Price,
                    DiscountAtMoment = 0,
                    Subtotal = subtotal
                });

                product.StockQuantity -= itemDto.Quantity;
            }

            order.TotalAmount = total;

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            _logger.LogInformation("Створено замовлення {OrderId} користувачем {UserId}", order.OrderId, userId);

            return order;
        }

        public async Task<List<Order>> GetMyOrdersAsync()
        {
            var userId = GetCurrentUserId();
            return await _orderRepository.GetByUserIdAsync(userId);
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _orderRepository.GetAllAsync();
        }

        public async Task<Order> GetByIdAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new NotFoundException("Замовлення не знайдено");

            return order;
        }

        public async Task<string> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new NotFoundException("Замовлення не знайдено");

            order.Status = dto.Status;
            await _orderRepository.SaveChangesAsync();

            return "Статус замовлення оновлено";
        }
    }
}