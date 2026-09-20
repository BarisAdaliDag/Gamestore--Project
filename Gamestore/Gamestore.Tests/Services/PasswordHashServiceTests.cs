using Gamestore.BLL.Services.Concrete;
using Gamestore.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Gamestore.Tests.Services;

public class PasswordHashServiceTests
{
    private const string Password = "SuperSecuredPassword";

    private readonly PasswordHashService _service = new(new PasswordHasher<User>());

    [Fact]
    public void HashPasswordShouldNotStorePlainTextPassword()
    {
        var user = CreateUser();

        var hash = _service.HashPassword(user, Password);

        Assert.NotEqual(Password, hash);
    }

    [Fact]
    public void HashPasswordShouldUseDifferentSaltForSamePassword()
    {
        var firstHash = _service.HashPassword(CreateUser(), Password);
        var secondHash = _service.HashPassword(CreateUser(), Password);

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void VerifyPasswordShouldReturnTrueForCorrectPassword()
    {
        var user = CreateUser();
        user.PasswordHash = _service.HashPassword(user, Password);

        var result = _service.VerifyPassword(user, Password);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPasswordShouldReturnFalseForIncorrectPassword()
    {
        var user = CreateUser();
        user.PasswordHash = _service.HashPassword(user, Password);

        var result = _service.VerifyPassword(user, "WrongPassword");

        Assert.False(result);
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = "TestUser",
            PasswordHash = string.Empty,
        };
    }
}
