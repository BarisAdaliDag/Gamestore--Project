using System.Linq.Expressions;
using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.DTOs.Users;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Constants;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Moq;

namespace Gamestore.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPasswordHashService> _passwordHashService = new();
    private readonly Mock<IRepository<Role>> _roles = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<UserRole>> _userRoles = new();
    private readonly Mock<IUserRepository> _users = new();

    public UserServiceTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_users.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Roles).Returns(_roles.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.UserRoles).Returns(_userRoles.Object);
    }

    [Fact]
    public async Task GetAllAsyncShouldReturnUsersWithoutPasswordHashes()
    {
        var user = CreateUser();
        var expected = new UserDto { Id = user.Id, Name = user.Name };
        _users.Setup(repository => repository.GetAllAsync()).ReturnsAsync([user]);
        _mapper.Setup(mapper => mapper.Map<IEnumerable<UserDto>>(It.IsAny<IEnumerable<User>>()))
            .Returns([expected]);
        var service = CreateService();

        var result = await service.GetAllAsync();

        var returnedUser = Assert.Single(result);
        Assert.Equal(expected.Id, returnedUser.Id);
        Assert.Equal(expected.Name, returnedUser.Name);
    }

    [Fact]
    public async Task GetByIdAsyncShouldThrowWhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        _users.Setup(repository => repository.GetByIdAsync(id)).ReturnsAsync((User?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(id));
    }

    [Fact]
    public async Task GetRolesAsyncShouldReturnAssignedRoles()
    {
        var user = CreateUser();
        var role = new Role { Id = Guid.NewGuid(), Name = "Administrator" };
        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            Role = role,
        });
        var expected = new RoleDto { Id = role.Id, Name = role.Name };
        _users.Setup(repository => repository.GetByIdWithRolesAsync(user.Id))
            .ReturnsAsync(user);
        _mapper.Setup(mapper => mapper.Map<IEnumerable<RoleDto>>(It.IsAny<IEnumerable<Role>>()))
            .Returns([expected]);
        var service = CreateService();

        var result = await service.GetRolesAsync(user.Id);

        var returnedRole = Assert.Single(result);
        Assert.Equal(expected.Id, returnedRole.Id);
        Assert.Equal(expected.Name, returnedRole.Name);
    }

    [Fact]
    public async Task GetRolesAsyncShouldThrowWhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        _users.Setup(repository => repository.GetByIdWithRolesAsync(id))
            .ReturnsAsync((User?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetRolesAsync(id));
    }

    [Fact]
    public async Task CreateAsyncShouldHashPasswordAndAssignRoles()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "User" };
        var dto = CreateDto(role.Id);
        _users.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        _roles.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync([role]);
        _passwordHashService.Setup(service => service.HashPassword(
                It.IsAny<User>(),
                dto.Password))
            .Returns("hashed-password");
        _mapper.Setup(mapper => mapper.Map<UserDto>(It.IsAny<User>()))
            .Returns<User>(user => new UserDto { Id = user.Id, Name = user.Name });
        var service = CreateService();

        var result = await service.CreateAsync(dto);

        Assert.Equal("NewUser", result.Name);
        _users.Verify(
            repository => repository.AddAsync(
                It.Is<User>(user => user.PasswordHash == "hashed-password")),
            Times.Once);
        _userRoles.Verify(
            repository => repository.AddAsync(
                It.Is<UserRole>(userRole => userRole.RoleId == role.Id)),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncShouldRejectDuplicateUserName()
    {
        var roleId = Guid.NewGuid();
        var dto = CreateDto(roleId);
        _users.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(CreateUser());
        var service = CreateService();

        await Assert.ThrowsAsync<EntityAlreadyExistsException>(() => service.CreateAsync(dto));

        _users.Verify(repository => repository.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsyncShouldRejectUnknownRole()
    {
        var dto = CreateDto(Guid.NewGuid());
        _users.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        _roles.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync([]);
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));

        _users.Verify(repository => repository.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldHashPasswordAndReplaceAssignedRoles()
    {
        var oldRoleId = Guid.NewGuid();
        var newRole = new Role { Id = Guid.NewGuid(), Name = "Manager" };
        var user = CreateUser();
        var oldUserRole = new UserRole
        {
            UserId = user.Id,
            RoleId = oldRoleId,
        };
        user.UserRoles.Add(oldUserRole);
        var dto = CreateUpdateDto(user.Id, newRole.Id);
        _users.Setup(repository => repository.GetByIdWithRolesForUpdateAsync(user.Id))
            .ReturnsAsync(user);
        _users.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        _roles.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync([newRole]);
        _passwordHashService.Setup(service => service.HashPassword(user, dto.Password))
            .Returns("updated-hash");
        _mapper.Setup(mapper => mapper.Map<UserDto>(user))
            .Returns(new UserDto { Id = user.Id, Name = dto.User.Name });
        var service = CreateService();

        var result = await service.UpdateAsync(dto);

        Assert.Equal("UpdatedUser", result.Name);
        Assert.Equal("UpdatedUser", user.Name);
        Assert.Equal("updated-hash", user.PasswordHash);
        _userRoles.Verify(repository => repository.Delete(oldUserRole), Times.Once);
        _userRoles.Verify(
            repository => repository.AddAsync(
                It.Is<UserRole>(userRole => userRole.UserId == user.Id && userRole.RoleId == newRole.Id)),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsyncShouldThrowWhenUserDoesNotExist()
    {
        var dto = CreateUpdateDto(Guid.NewGuid(), Guid.NewGuid());
        _users.Setup(repository => repository.GetByIdWithRolesForUpdateAsync(dto.User.Id))
            .ReturnsAsync((User?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldRejectDuplicateUserName()
    {
        var user = CreateUser();
        var dto = CreateUpdateDto(user.Id, Guid.NewGuid());
        _users.Setup(repository => repository.GetByIdWithRolesForUpdateAsync(user.Id))
            .ReturnsAsync(user);
        _users.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Name = dto.User.Name,
                PasswordHash = "another-hash",
            });
        var service = CreateService();

        await Assert.ThrowsAsync<EntityAlreadyExistsException>(() => service.UpdateAsync(dto));

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldRejectUnknownRole()
    {
        var user = CreateUser();
        var dto = CreateUpdateDto(user.Id, Guid.NewGuid());
        _users.Setup(repository => repository.GetByIdWithRolesForUpdateAsync(user.Id))
            .ReturnsAsync(user);
        _users.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        _roles.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync([]);
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(dto));

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldRejectDemotingLastAdministrator()
    {
        var administratorRole = new Role { Id = Guid.NewGuid(), Name = RoleNames.Administrator };
        var userRole = new Role { Id = Guid.NewGuid(), Name = RoleNames.User };
        var user = CreateUser();
        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = administratorRole.Id,
            Role = administratorRole,
        });
        var dto = CreateUpdateDto(user.Id, userRole.Id);
        _users.Setup(repository => repository.GetByIdWithRolesForUpdateAsync(user.Id))
            .ReturnsAsync(user);
        _users.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);
        _roles.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync([userRole]);
        _users.Setup(repository => repository.CountByRoleNameAsync(RoleNames.Administrator))
            .ReturnsAsync(1);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(dto));

        Assert.Equal(
            "The last Administrator user cannot lose the Administrator role.",
            exception.Message);
        _userRoles.Verify(repository => repository.Delete(It.IsAny<UserRole>()), Times.Never);
        _userRoles.Verify(repository => repository.AddAsync(It.IsAny<UserRole>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsyncShouldDeleteUserAndAssignedRoles()
    {
        var user = CreateUser();
        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = Guid.NewGuid(),
            Role = new Role { Id = Guid.NewGuid(), Name = RoleNames.User },
        };
        user.UserRoles.Add(userRole);
        _users.Setup(repository => repository.GetByIdWithRolesForUpdateAsync(user.Id))
            .ReturnsAsync(user);
        var service = CreateService();

        await service.DeleteAsync(user.Id);

        _userRoles.Verify(repository => repository.Delete(userRole), Times.Once);
        _users.Verify(repository => repository.Delete(user), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncShouldThrowWhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        _users.Setup(repository => repository.GetByIdWithRolesForUpdateAsync(id))
            .ReturnsAsync((User?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(id));

        _users.Verify(repository => repository.Delete(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsyncShouldRejectDeletingLastAdministrator()
    {
        var user = CreateUser();
        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = Guid.NewGuid(),
            Role = new Role { Id = Guid.NewGuid(), Name = RoleNames.Administrator },
        });
        _users.Setup(repository => repository.GetByIdWithRolesForUpdateAsync(user.Id))
            .ReturnsAsync(user);
        _users.Setup(repository => repository.CountByRoleNameAsync(RoleNames.Administrator))
            .ReturnsAsync(1);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(user.Id));

        Assert.Equal("The last Administrator user cannot be deleted.", exception.Message);
        _users.Verify(repository => repository.Delete(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = "Administrator",
            PasswordHash = "secret-hash",
        };
    }

    private static CreateUserDto CreateDto(Guid roleId)
    {
        return new CreateUserDto
        {
            User = new UserCreateInfoDto { Name = "NewUser" },
            Roles = [roleId],
            Password = "SecurePassword",
        };
    }

    private static UpdateUserDto CreateUpdateDto(Guid userId, Guid roleId)
    {
        return new UpdateUserDto
        {
            User = new UserUpdateInfoDto { Id = userId, Name = "UpdatedUser" },
            Roles = [roleId],
            Password = "UpdatedPassword",
        };
    }

    private UserService CreateService()
    {
        return new UserService(
            _unitOfWork.Object,
            _mapper.Object,
            _passwordHashService.Object);
    }
}
