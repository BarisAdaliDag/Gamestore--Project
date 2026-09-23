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
    private readonly Mock<IExternalAuthenticationService> _externalAuthenticationService = new();
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
        _externalAuthenticationService.Verify(
            externalService => externalService.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
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
    public async Task LoginAsyncShouldReturnTokenForValidExternalCredentials()
    {
        var user = CreateUser("user@example.com");
        _users.Setup(repository => repository.GetByNameWithRolesAsync(user.Name))
            .ReturnsAsync(user);
        _externalAuthenticationService
            .Setup(externalService => externalService.AuthenticateAsync(user.Name, "ExternalPassword"))
            .ReturnsAsync(true);
        _jwtTokenService.Setup(service => service.GenerateToken(user))
            .Returns("external-token");
        var service = CreateService();
        var dto = CreateDto(user.Name, "ExternalPassword");
        dto.InternalAuth = false;

        var token = await service.LoginAsync(dto);

        Assert.Equal("external-token", token);
        _passwordHashService.Verify(
            passwordService => passwordService.VerifyPassword(It.IsAny<User>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsyncShouldRejectInvalidExternalCredentials()
    {
        var user = CreateUser("user@example.com");
        _users.Setup(repository => repository.GetByNameWithRolesAsync(user.Name))
            .ReturnsAsync(user);
        _externalAuthenticationService
            .Setup(externalService => externalService.AuthenticateAsync(user.Name, "WrongPassword"))
            .ReturnsAsync(false);
        var service = CreateService();
        var dto = CreateDto(user.Name, "WrongPassword");
        dto.InternalAuth = false;

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(dto));

        _jwtTokenService.Verify(service => service.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsyncShouldNotCallExternalServiceWhenLocalUserDoesNotExist()
    {
        _users.Setup(repository => repository.GetByNameWithRolesAsync("missing@example.com"))
            .ReturnsAsync((User?)null);
        var service = CreateService();
        var dto = CreateDto("missing@example.com", "Password");
        dto.InternalAuth = false;

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(dto));

        _externalAuthenticationService.Verify(
            externalService => externalService.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>()),
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

    private static User CreateUser(string name = "Administrator")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            PasswordHash = "password-hash",
        };
    }

    private UserAuthenticationService CreateService()
    {
        return new UserAuthenticationService(
            _unitOfWork.Object,
            _passwordHashService.Object,
            _jwtTokenService.Object,
            _externalAuthenticationService.Object);
    }
}
