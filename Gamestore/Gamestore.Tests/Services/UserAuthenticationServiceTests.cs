using Gamestore.BLL.DTOs.Users;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using Moq;

namespace Gamestore.Tests.Services;

public class UserAuthenticationServiceTests
{
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly Mock<IPasswordHashService> _passwordHashService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();

    public UserAuthenticationServiceTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_users.Object);
    }

    [Fact]
    public async Task LoginAsyncShouldReturnTokenForValidCredentials()
    {
        var user = CreateUser();
        _users.Setup(repository => repository.GetByNameWithRolesAsync(user.Name))
            .ReturnsAsync(user);
        _passwordHashService.Setup(service => service.VerifyPassword(user, "CorrectPassword"))
            .Returns(true);
        _jwtTokenService.Setup(service => service.GenerateToken(user))
            .Returns("generated-token");
        var service = CreateService();

        var token = await service.LoginAsync(CreateDto(user.Name, "CorrectPassword"));

        Assert.Equal("generated-token", token);
    }

    [Fact]
    public async Task LoginAsyncShouldRejectUnknownUser()
    {
        _users.Setup(repository => repository.GetByNameWithRolesAsync("Unknown"))
            .ReturnsAsync((User?)null);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.LoginAsync(CreateDto("Unknown", "Password")));

        Assert.Equal("The login or password is incorrect.", exception.Message);
        _jwtTokenService.Verify(service => service.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsyncShouldRejectIncorrectPassword()
    {
        var user = CreateUser();
        _users.Setup(repository => repository.GetByNameWithRolesAsync(user.Name))
            .ReturnsAsync(user);
        _passwordHashService.Setup(service => service.VerifyPassword(user, "WrongPassword"))
            .Returns(false);
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.LoginAsync(CreateDto(user.Name, "WrongPassword")));

        _jwtTokenService.Verify(service => service.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsyncShouldRejectExternalAuthentication()
    {
        var service = CreateService();
        var dto = CreateDto("Administrator", "Password");
        dto.InternalAuth = false;

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.LoginAsync(dto));

        Assert.Equal("External authentication is not supported.", exception.Message);
        _users.Verify(
            repository => repository.GetByNameWithRolesAsync(It.IsAny<string>()),
            Times.Never);
    }

    private static LoginDto CreateDto(string login, string password)
    {
        return new LoginDto
        {
            Login = login,
            Password = password,
            InternalAuth = true,
        };
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = "Administrator",
            PasswordHash = "password-hash",
        };
    }

    private UserAuthenticationService CreateService()
    {
        return new UserAuthenticationService(
            _unitOfWork.Object,
            _passwordHashService.Object,
            _jwtTokenService.Object);
    }
}
