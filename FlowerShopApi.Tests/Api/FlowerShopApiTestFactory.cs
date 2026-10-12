
using FlowerShopApi.Controllers;
using FlowerShopApi.Data;
using FlowerShopApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowerShopApi.Tests.Api;

public sealed class FlowerShopApiTestFactory
    : WebApplicationFactory<AuthController>
{
    // Одна унікальна база для кожного екземпляра фабрики
    private readonly string _databaseName =
        $"api_test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] =
                        "ApiTestsOnlySigningKey_1234567890_ABCDEF_1234567890",
                    ["Jwt:Issuer"] = "FlowerShopApi.Tests",
                    ["Jwt:Audience"] = "FlowerShopApi.Tests"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            // Видаляємо оригінальну реєстрацію MySQL
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<AppDbContext>>();

            // Додаємо InMemory замість MySQL
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }

    public async Task SeedCustomerRoleAsync()
    {
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await db.Roles.AddAsync(new Role
        {
            RoleId = 1,
            RoleName = "customer"
        });

        await db.SaveChangesAsync();
    }
}
