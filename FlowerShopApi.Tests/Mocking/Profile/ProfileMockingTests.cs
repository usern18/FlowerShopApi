using FlowerShopApi.DTOs.Profile;
using FlowerShopApi.Models;
using FlowerShopApi.Repositories;
using FlowerShopApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace FlowerShopApi.Tests.Mocking.Profile;

[TestFixture]
public class ProfileMockingTests
{
    [Test]
    public async Task UpdateDeliveryAddressAsync_ShouldUpdateRepositoryOnce_WithTrimmedAddress()
    {
        var user = new User { UserId = 20, FirstName = "Jane", LastName = "Test", Email = "jane@example.com" };
        var repo = new Mock<IUserRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetByIdAsync(20)).ReturnsAsync(user);
        repo.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        var service = new ProfileService(repo.Object, NullLogger<ProfileService>.Instance);
        var dto = new UpdateDeliveryAddressDto
        {
            Region = "  Чернівецька  ", City = "  Чернівці  ",
            StreetAddress = "  Головна, 1  ", PostalCode = "  58000  "
        };

        var result = await service.UpdateDeliveryAddressAsync(20, dto);

        Assert.That(result.City, Is.EqualTo("Чернівці"));
        Assert.That(result.StreetAddress, Is.EqualTo("Головна, 1"));
        repo.Verify(x => x.GetByIdAsync(20), Times.Once);
        repo.Verify(x => x.UpdateAsync(It.Is<User>(u => u.UserId == 20 &&
            u.Region == "Чернівецька" && u.City == "Чернівці" &&
            u.StreetAddress == "Головна, 1" && u.PostalCode == "58000")), Times.Once);
        repo.VerifyNoOtherCalls();
    }
}
