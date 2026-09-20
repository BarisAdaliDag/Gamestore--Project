using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.DTOs.Users;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Moq;

namespace Gamestore.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();

    public UserServiceTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_users.Object);
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

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = "Administrator",
            PasswordHash = "secret-hash",
        };
    }

    private UserService CreateService()
    {
        return new UserService(_unitOfWork.Object, _mapper.Object);
    }
}
