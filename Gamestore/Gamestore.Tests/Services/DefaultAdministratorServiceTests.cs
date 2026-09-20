using System.Linq.Expressions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Constants;
using Gamestore.Domain.Entities;
using Moq;

namespace Gamestore.Tests.Services;

public class DefaultAdministratorServiceTests
{
    private static readonly Guid AdministratorRoleId = Guid.NewGuid();

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRepository<Role>> _roles = new();
    private readonly Mock<IRepository<UserRole>> _userRoles = new();
    private readonly Mock<IPasswordHashService> _passwordHashService = new();

    public DefaultAdministratorServiceTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Roles).Returns(_roles.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.UserRoles).Returns(_userRoles.Object);

        _roles.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync(new Role
            {
                Id = AdministratorRoleId,
                Name = RoleNames.Administrator,
            });
    }

    [Fact]
    public async Task EnsureCreatedAsyncShouldCreateAdministratorWhenItDoesNotExist()
    {
        _users.Setup(repository => repository.GetByNameWithRolesAsync("Administrator"))
            .ReturnsAsync((User?)null);
        _passwordHashService.Setup(service => service.HashPassword(
                It.IsAny<User>(),
                "SecurePassword"))
            .Returns("password-hash");
        var service = CreateService();

        var changed = await service.EnsureCreatedAsync("Administrator", "SecurePassword");

        Assert.True(changed);
        _users.Verify(
            repository => repository.AddAsync(
                It.Is<User>(user =>
                    user.Name == "Administrator"
                    && user.PasswordHash == "password-hash")),
            Times.Once);
        _userRoles.Verify(
            repository => repository.AddAsync(
                It.Is<UserRole>(userRole => userRole.RoleId == AdministratorRoleId)),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task EnsureCreatedAsyncShouldNotChangeExistingAdministrator()
    {
        _users.Setup(repository => repository.GetByNameWithRolesAsync("Administrator"))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Name = "Administrator",
                PasswordHash = "password-hash",
                UserRoles =
                [
                    new UserRole { RoleId = AdministratorRoleId },
                ],
            });
        var service = CreateService();

        var changed = await service.EnsureCreatedAsync("Administrator", "SecurePassword");

        Assert.False(changed);
        _users.Verify(repository => repository.AddAsync(It.IsAny<User>()), Times.Never);
        _userRoles.Verify(repository => repository.AddAsync(It.IsAny<UserRole>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task EnsureCreatedAsyncShouldThrowWhenAdministratorRoleDoesNotExist()
    {
        _roles.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync((Role?)null);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EnsureCreatedAsync("Administrator", "SecurePassword"));

        Assert.Equal("The Administrator role is not configured.", exception.Message);
    }

    private DefaultAdministratorService CreateService()
    {
        return new DefaultAdministratorService(
            _unitOfWork.Object,
            _passwordHashService.Object);
    }
}
