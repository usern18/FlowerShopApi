using FlowerShopApi.Data;
using FlowerShopApi.DTOs.Users;
using FlowerShopApi.Exceptions;
using FlowerShopApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace FlowerShopApi.Tests.Exceptions.Users;

[TestFixture]
public class UserExceptionTests
{
    [Test]
    public async Task UpdateProfileAsync_ShouldThrowUnauthorizedException_WhenUserIsNotAuthenticated()
    {
        // Arrange: HttpContext без claim NameIdentifier
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AppDbContext(options);
        var httpAccessor = new Mock<IHttpContextAccessor>();
        httpAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());
        var service = new UserService(context, httpAccessor.Object);
        var dto = new UpdateProfileDto
        {
            FirstName = "New",
            LastName = "User",
            Phone = "0991234567"
        };

        // Act + Assert
        var exception = Assert.ThrowsAsync<UnauthorizedException>(
            async () => await service.UpdateProfileAsync(dto));
        Assert.That(exception!.Message, Is.EqualTo("Користувач не авторизований"));
        Assert.That(context.ChangeTracker.HasChanges(), Is.False);
    }
}
