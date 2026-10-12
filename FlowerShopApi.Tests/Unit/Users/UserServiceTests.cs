using System.Security.Claims;
using FlowerShopApi.Data;
using FlowerShopApi.DTOs.Users;
using FlowerShopApi.Models;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace FlowerShopApi.Tests.Unit.Users;

[TestFixture]
public class UserServiceTests
{
    [Test]
    public async Task UpdateProfileAsync_ShouldUpdateUser_WhenDataIsValid()
    {
        // Arrange: контекст у пам'яті замість MySQL
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AppDbContext(options);
        context.Roles.Add(new Role { RoleId = 1, RoleName = "customer" });
        context.Users.Add(new User
        {
            UserId = 10,
            RoleId = 1,
            FirstName = "Old",
            LastName = "Name",
            Phone = "0991111111",
            Email = "test@example.com",
            PasswordHash = "not-used-in-this-test"
        });
        await context.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10")
        }, "TestAuthentication"));
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new UserService(context, accessor);
        var dto = new UpdateProfileDto
        {
            FirstName = "New",
            LastName = "Surname",
            Phone = "0992222222"
        };

        // Act
        var result = await service.UpdateProfileAsync(dto);

        // Assert: AsNoTracking читає фактично збережений стан, а не tracked entity
        var updated = await context.Users.AsNoTracking().SingleAsync(u => u.UserId == 10);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("Профіль оновлено"));
            Assert.That(updated.FirstName, Is.EqualTo(dto.FirstName));
            Assert.That(updated.LastName, Is.EqualTo(dto.LastName));
            Assert.That(updated.Phone, Is.EqualTo(dto.Phone));
        });
    }
}
