using FlowerShopApi.Data;
using FlowerShopApi.DTOs.Auth;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace FlowerShopApi.Tests.Mocking.Auth;

[TestFixture]
public class AuthMockingTests
{
    [Test]
    public async Task RegisterAsync_ShouldCheckUniquenessAndAddUserOnce_WhenDataIsValid()
    {
        var dto = new RegisterDto { FirstName = "Jane", LastName = "Test", Email = "jane@example.com", Phone = "0991234567", Password = "Password123!" };
        await using var context = CreateContext();
        context.Roles.Add(new Role { RoleId = 1, RoleName = "customer" });
        await context.SaveChangesAsync();
        var repo = new Mock<IUserRepository>(MockBehavior.Strict);
        repo.Setup(x => x.EmailExistsAsync(dto.Email)).ReturnsAsync(false);
        repo.Setup(x => x.PhoneExistsAsync(dto.Phone)).ReturnsAsync(false);
        repo.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        repo.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        var service = CreateService(context, repo);

        await service.RegisterAsync(dto);

        repo.Verify(x => x.EmailExistsAsync(dto.Email), Times.Once);
        repo.Verify(x => x.PhoneExistsAsync(dto.Phone), Times.Once);
        repo.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == dto.Email && u.RoleId == 1 && u.PasswordHash != dto.Password)), Times.Once);
        repo.Verify(x => x.SaveChangesAsync(), Times.Once);
        repo.VerifyNoOtherCalls();
    }

    [Test]
    public async Task LoginAsync_ShouldUseTokenServiceOnce_WhenPasswordIsValid()
    {
        const string password = "Password123!";
        var user = new User { UserId = 5, Email = "jane@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Role = new Role { RoleId = 1, RoleName = "customer" } };
        var repo = new Mock<IUserRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        repo.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        var tokens = new Mock<ITokenService>(MockBehavior.Strict);
        tokens.Setup(x => x.GenerateAccessToken(user)).Returns("access-example");
        tokens.Setup(x => x.GenerateRefreshToken()).Returns("refresh-example");
        await using var context = CreateContext();
        var service = CreateService(context, repo, tokens);

        var result = await service.LoginAsync(new LoginDto { Email = user.Email, Password = password });

        Assert.That(result.AccessToken, Is.EqualTo("access-example"));
        Assert.That(user.RefreshToken, Is.EqualTo("refresh-example"));
        repo.Verify(x => x.GetByEmailAsync(user.Email), Times.Once);
        repo.Verify(x => x.SaveChangesAsync(), Times.Once);
        tokens.Verify(x => x.GenerateAccessToken(user), Times.Once);
        tokens.Verify(x => x.GenerateRefreshToken(), Times.Once);
        repo.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GoogleLoginAsync_ShouldValidateGoogleTokenAndFindExistingUser_WhenAccountExists()
    {
        var payload = new GoogleJsonWebSignature.Payload { Email = "google@example.com", Subject = "google-subject-123" };
        var user = new User { UserId = 7, Email = payload.Email, Role = new Role { RoleId = 1, RoleName = "customer" }, OAuthProvider = "Google", OAuthProviderUserId = payload.Subject };
        var google = new Mock<IGoogleAuthService>(MockBehavior.Strict);
        google.Setup(x => x.ValidateGoogleTokenAsync("test-google-id-token")).ReturnsAsync(payload);
        var repo = new Mock<IUserRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetByEmailOrOAuthAsync(payload.Email, "Google", payload.Subject)).ReturnsAsync(user);
        repo.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
        var tokens = new Mock<ITokenService>(MockBehavior.Strict);
        tokens.Setup(x => x.GenerateAccessToken(user)).Returns("access-example");
        tokens.Setup(x => x.GenerateRefreshToken()).Returns("refresh-example");
        await using var context = CreateContext();
        var service = CreateService(context, repo, tokens, google);

        var result = await service.GoogleLoginAsync(new GoogleLoginDto { IdToken = "test-google-id-token" });

        Assert.That(result.Email, Is.EqualTo(user.Email));
        google.Verify(x => x.ValidateGoogleTokenAsync("test-google-id-token"), Times.Once);
        repo.Verify(x => x.GetByEmailOrOAuthAsync(payload.Email, "Google", payload.Subject), Times.Once);
        repo.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        repo.Verify(x => x.SaveChangesAsync(), Times.Once);
        tokens.Verify(x => x.GenerateAccessToken(user), Times.Once);
        tokens.Verify(x => x.GenerateRefreshToken(), Times.Once);
        google.VerifyNoOtherCalls();
        repo.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
    }

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AuthService CreateService(AppDbContext context, Mock<IUserRepository> repo,
        Mock<ITokenService>? tokens = null, Mock<IGoogleAuthService>? google = null)
        => new(repo.Object, (tokens ?? new Mock<ITokenService>()).Object,
            (google ?? new Mock<IGoogleAuthService>()).Object, new Mock<ICurrentUserService>().Object,
            NullLogger<AuthService>.Instance, context);
}
