using FlowerShopApi.DTOs.Profile;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace FlowerShopApi.Tests.Unit.Profile;

[TestFixture]
public class ProfileServiceTests
{
    [Test]
    public async Task UpdatePersonalInfoAsync_ShouldUpdatePersonalData_WhenUserExists()
    {
        // Arrange
        var user = new User { UserId = 10, Email = "test@example.com", FirstName = "Old", LastName = "Name" };
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        var service = new ProfileService(repository.Object, NullLogger<ProfileService>.Instance);
        var dto = new UpdatePersonalInfoDto
        {
            FirstName = "  New  ", LastName = "  Surname  ",
            MiddleName = "  Middle  ", Phone = "  0991234567  "
        };

        // Act
        var result = await service.UpdatePersonalInfoAsync(10, dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.FirstName, Is.EqualTo("New"));
            Assert.That(result.LastName, Is.EqualTo("Surname"));
            Assert.That(result.MiddleName, Is.EqualTo("Middle"));
            Assert.That(result.Phone, Is.EqualTo("0991234567"));
        });
        repository.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.UserId == 10 && u.FirstName == "New" &&
            u.LastName == "Surname" && u.MiddleName == "Middle" &&
            u.Phone == "0991234567")), Times.Once);
    }

    [Test]
    public async Task ChangePasswordAsync_ShouldChangePassword_WhenCurrentPasswordIsCorrect()
    {
        // Arrange
        const string oldPassword = "OldPassword123!";
        const string newPassword = "NewPassword456!";
        var oldHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);
        var user = new User { UserId = 10, Email = "test@example.com", PasswordHash = oldHash };
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        var service = new ProfileService(repository.Object, NullLogger<ProfileService>.Instance);
        var dto = new ChangePasswordDto { CurrentPassword = oldPassword, NewPassword = newPassword };

        // Act
        await service.ChangePasswordAsync(10, dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(user.PasswordHash, Is.Not.EqualTo(oldHash));
            Assert.That(user.PasswordHash, Is.Not.EqualTo(newPassword));
            Assert.That(BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash), Is.True);
            Assert.That(BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash), Is.False);
        });
        repository.Verify(r => r.UpdateAsync(It.Is<User>(u => u.UserId == 10)), Times.Once);
    }
}
