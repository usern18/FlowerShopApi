using FlowerShopApi.Data;
using FlowerShopApi.DTOs.Auth;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace FlowerShopApi.Tests.Unit.Auth;

[TestFixture]
public class AuthServiceTests
{
    [Test]
    public async Task RegisterAsync_ShouldRegisterUser_WhenDataIsValid()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Phone = "0991234567",
            Password = "Password123!"
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AppDbContext(options);
        context.Roles.Add(new Role { RoleId = 1, RoleName = "customer" });
        await context.SaveChangesAsync();

        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        repository.Setup(r => r.PhoneExistsAsync(dto.Phone)).ReturnsAsync(false);

        var service = CreateService(repository, new Mock<ITokenService>(), context);

        // Act
        var result = await service.RegisterAsync(dto);

        // Assert
        Assert.That(result, Is.EqualTo("Реєстрація успішна"));
        repository.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email == dto.Email &&
            u.FirstName == dto.FirstName &&
            u.LastName == dto.LastName &&
            u.Phone == dto.Phone &&
            u.RoleId == 1 &&
            u.IsActive &&
            u.PasswordHash != dto.Password)), Times.Once);
        repository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var dto = new LoginDto { Email = "test@example.com", Password = "Password123!" };
        var user = new User
        {
            UserId = 10,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FirstName = "Test",
            LastName = "User",
            RoleId = 1,
            Role = new Role { RoleId = 1, RoleName = "customer" },
            IsActive = true
        };
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);

        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.GenerateAccessToken(user)).Returns("test-access-token");
        tokens.Setup(t => t.GenerateRefreshToken()).Returns("test-refresh-token");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AppDbContext(options);
        var service = CreateService(repository, tokens, context);

        // Act
        var result = await service.LoginAsync(dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.AccessToken, Is.EqualTo("test-access-token"));
            Assert.That(result.RefreshToken, Is.EqualTo("test-refresh-token"));
            Assert.That(result.Email, Is.EqualTo(dto.Email));
            Assert.That(result.Role, Is.EqualTo("customer"));
            Assert.That(user.RefreshToken, Is.EqualTo("test-refresh-token"));
            Assert.That(user.RefreshTokenExpiryTime, Is.Not.Null);
        });
        repository.Verify(r => r.GetByEmailAsync(dto.Email), Times.Once);
        repository.Verify(r => r.SaveChangesAsync(), Times.Once);
        tokens.Verify(t => t.GenerateAccessToken(user), Times.Once);
        tokens.Verify(t => t.GenerateRefreshToken(), Times.Once);
    }

    private static AuthService CreateService(
        Mock<IUserRepository> repository,
        Mock<ITokenService> tokens,
        AppDbContext context)
    {
        return new AuthService(
            repository.Object,
            tokens.Object,
            new Mock<IGoogleAuthService>().Object,
            new Mock<ICurrentUserService>().Object,
            NullLogger<AuthService>.Instance,
            context);
    }
}
