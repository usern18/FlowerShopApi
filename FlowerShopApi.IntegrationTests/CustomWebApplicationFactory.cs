using FlowerShopApi.Data;
using FlowerShopApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowerShopApi.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"OrdersTests_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Повністю видаляємо все, що стосується попередніх налаштувань AppDbContext та провайдерів
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ServiceType.FullName?.Contains("DbContextOptions") == true ||
                    d.ImplementationType == typeof(AppDbContext))
                .ToList();

            foreach (var d in descriptorsToRemove)
            {
                services.Remove(d);
            }

            // Додаємо InMemory базу для тестів
            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(_dbName));
        });
    }

    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        Seed(db);
    }

    private static void Seed(AppDbContext db)
    {
        db.Roles.AddRange(
            new Role { RoleId = 1, RoleName = "customer" },
            new Role { RoleId = 2, RoleName = "admin" });

        db.Categories.Add(new Category { CategoryId = 1, CategoryName = "Троянди" });

        db.Products.Add(new Product
        {
            ProductId = 1,
            CategoryId = 1,
            Name = "Букет троянд",
            Description = "12 троянд",
            Price = 350,
            StockQuantity = 100,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        db.Users.Add(new User
        {
            UserId = 1,
            RoleId = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Phone = "+380501111111",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        db.Users.Add(new User
        {
            UserId = 2,
            RoleId = 2,
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@example.com",
            Phone = "+380502222222",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        db.SaveChanges();
    }
}