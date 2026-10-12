using FlowerShopApi.Data;
using FlowerShopApi.DTOs.Auth;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace FlowerShopApi.Tests.Exceptions.Auth;

[TestFixture]
public class AuthExceptionTests
{
    [Test]
    public async Task LoginAsync_ShouldThrowUnauthorizedException_WhenPasswordIsIncorrect()
    {
        // Arrange
        var dto = new LoginDto { Email = "test@example.com", Password = "WrongPassword123!" };
        var user = new User
        {
            UserId = 10,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!")
        };
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        var tokens = new Mock<ITokenService>();
        await using var context = CreateContext();
        var service = CreateService(repository, tokens, new Mock<ICurrentUserService>(), context);

        // Act + Assert
        var exception = Assert.ThrowsAsync<UnauthorizedException>(
            async () => await service.LoginAsync(dto));
        Assert.That(exception!.Message, Is.EqualTo("Невірний email або пароль"));
        tokens.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        repository.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task MeAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        const int userId = 999;
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.GetCurrentUserId()).Returns(userId);
        await using var context = CreateContext();
        var service = CreateService(repository, new Mock<ITokenService>(), currentUser, context);

        // Act + Assert
        var exception = Assert.ThrowsAsync<NotFoundException>(
            async () => await service.MeAsync());
        Assert.That(exception!.Message, Is.EqualTo("Користувача не знайдено"));
        repository.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [Test]
    public async Task RefreshTokenAsync_ShouldThrowUnauthorizedException_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var dto = new RefreshTokenRequestDto { RefreshToken = "invalid-refresh-token" };
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByRefreshTokenAsync(dto.RefreshToken)).ReturnsAsync((User?)null);
        var tokens = new Mock<ITokenService>();
        await using var context = CreateContext();
        var service = CreateService(repository, tokens, new Mock<ICurrentUserService>(), context);

        // Act + Assert
        var exception = Assert.ThrowsAsync<UnauthorizedException>(
            async () => await service.RefreshTokenAsync(dto));
        Assert.That(exception!.Message, Is.EqualTo("Недійсний або протермінований refresh token"));
        repository.Verify(r => r.GetByRefreshTokenAsync(dto.RefreshToken), Times.Once);
        tokens.Verify(t => t.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AuthService CreateService(
        Mock<IUserRepository> repository,
        Mock<ITokenService> tokens,
        Mock<ICurrentUserService> currentUser,
        AppDbContext context)
    {
        return new AuthService(
            repository.Object,
            tokens.Object,
            new Mock<IGoogleAuthService>().Object,
            currentUser.Object,
            NullLogger<AuthService>.Instance,
            context);
    }
}
