using System.Linq.Expressions;
using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Constants;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Moq;

namespace Gamestore.Tests.Services;

public class RoleServiceTests
{
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IRepository<Role>> _roles = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<UserRole>> _userRoles = new();

    public RoleServiceTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Roles).Returns(_roles.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.UserRoles).Returns(_userRoles.Object);
    }

    [Fact]
    public async Task GetAllAsyncShouldReturnRolesInAlphabeticalOrder()
    {
        var roles = new List<Role>
        {
            new() { Id = Guid.NewGuid(), Name = "User" },
            new() { Id = Guid.NewGuid(), Name = "Administrator" },
        };
        _roles.Setup(repository => repository.GetAllAsync()).ReturnsAsync(roles);
        _mapper.Setup(mapper => mapper.Map<IEnumerable<RoleDto>>(It.IsAny<IEnumerable<Role>>()))
            .Returns<IEnumerable<Role>>(source => source.Select(role => new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
            }));
        var service = CreateService();

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal("Administrator", result[0].Name);
        Assert.Equal("User", result[1].Name);
    }

    [Fact]
    public async Task GetByIdAsyncShouldReturnRole()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Manager" };
        var expected = new RoleDto { Id = role.Id, Name = role.Name };
        _roles.Setup(repository => repository.GetByIdAsync(role.Id)).ReturnsAsync(role);
        _mapper.Setup(mapper => mapper.Map<RoleDto>(role)).Returns(expected);
        var service = CreateService();

        var result = await service.GetByIdAsync(role.Id);

        Assert.Equal(expected.Id, result.Id);
        Assert.Equal(expected.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsyncShouldThrowWhenRoleDoesNotExist()
    {
        var id = Guid.NewGuid();
        _roles.Setup(repository => repository.GetByIdAsync(id)).ReturnsAsync((Role?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(id));
    }

    [Fact]
    public async Task CreateAsyncShouldCreateRole()
    {
        var dto = new CreateRoleDto
        {
            Role = new RoleCreateInfoDto { Name = " Content Editor " },
        };
        _roles.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync((Role?)null);
        _mapper.Setup(mapper => mapper.Map<RoleDto>(It.IsAny<Role>()))
            .Returns<Role>(role => new RoleDto { Id = role.Id, Name = role.Name });
        var service = CreateService();

        var result = await service.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Content Editor", result.Name);
        _roles.Verify(
            repository => repository.AddAsync(
                It.Is<Role>(role => role.Name == "Content Editor")),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncShouldRejectDuplicateRoleName()
    {
        var dto = new CreateRoleDto
        {
            Role = new RoleCreateInfoDto { Name = "Administrator" },
        };
        _roles.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync(new Role { Id = Guid.NewGuid(), Name = "Administrator" });
        var service = CreateService();

        await Assert.ThrowsAsync<EntityAlreadyExistsException>(() => service.CreateAsync(dto));

        _roles.Verify(repository => repository.AddAsync(It.IsAny<Role>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldRenameCustomRole()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Content Editor" };
        var dto = CreateUpdateDto(role.Id, " Senior Editor ");
        _roles.Setup(repository => repository.GetByIdAsync(role.Id)).ReturnsAsync(role);
        _roles.Setup(repository =>
                repository.GetSingleAsync(It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync((Role?)null);
        _mapper.Setup(mapper => mapper.Map<RoleDto>(role))
            .Returns<Role>(source => new RoleDto { Id = source.Id, Name = source.Name });
        var service = CreateService();

        var result = await service.UpdateAsync(dto);

        Assert.Equal("Senior Editor", result.Name);
        _roles.Verify(repository => repository.Update(role), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsyncShouldThrowWhenRoleDoesNotExist()
    {
        var dto = CreateUpdateDto(Guid.NewGuid(), "Senior Editor");
        _roles.Setup(repository => repository.GetByIdAsync(dto.Role.Id)).ReturnsAsync((Role?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));

        _roles.Verify(repository => repository.Update(It.IsAny<Role>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldRejectDuplicateRoleName()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Content Editor" };
        var dto = CreateUpdateDto(role.Id, "Manager");
        _roles.Setup(repository => repository.GetByIdAsync(role.Id)).ReturnsAsync(role);
        _roles.Setup(repository =>
                repository.GetSingleAsync(It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync(new Role { Id = Guid.NewGuid(), Name = "Manager" });
        var service = CreateService();

        await Assert.ThrowsAsync<EntityAlreadyExistsException>(() => service.UpdateAsync(dto));

        _roles.Verify(repository => repository.Update(It.IsAny<Role>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldRejectRenamingDefaultRole()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = RoleNames.Administrator };
        var dto = CreateUpdateDto(role.Id, "Super Administrator");
        _roles.Setup(repository => repository.GetByIdAsync(role.Id)).ReturnsAsync(role);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(dto));

        Assert.Equal("Default roles cannot be renamed.", exception.Message);
        _roles.Verify(repository => repository.Update(It.IsAny<Role>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsyncShouldDeleteUnusedCustomRole()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Content Editor" };
        _roles.Setup(repository => repository.GetByIdAsync(role.Id)).ReturnsAsync(role);
        _userRoles.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<UserRole, bool>>>()))
            .ReturnsAsync([]);
        var service = CreateService();

        await service.DeleteAsync(role.Id);

        _roles.Verify(repository => repository.Delete(role), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncShouldThrowWhenRoleDoesNotExist()
    {
        var id = Guid.NewGuid();
        _roles.Setup(repository => repository.GetByIdAsync(id)).ReturnsAsync((Role?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(id));

        _roles.Verify(repository => repository.Delete(It.IsAny<Role>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsyncShouldRejectDeletingDefaultRole()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = RoleNames.Manager };
        _roles.Setup(repository => repository.GetByIdAsync(role.Id)).ReturnsAsync(role);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(role.Id));

        Assert.Equal("Default roles cannot be deleted.", exception.Message);
        _roles.Verify(repository => repository.Delete(It.IsAny<Role>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsyncShouldRejectDeletingAssignedRole()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Content Editor" };
        _roles.Setup(repository => repository.GetByIdAsync(role.Id)).ReturnsAsync(role);
        _userRoles.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<UserRole, bool>>>()))
            .ReturnsAsync([
                new UserRole { UserId = Guid.NewGuid(), RoleId = role.Id },
            ]);
        var service = CreateService();

        await Assert.ThrowsAsync<EntityInUseException>(() => service.DeleteAsync(role.Id));

        _roles.Verify(repository => repository.Delete(It.IsAny<Role>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    private static UpdateRoleDto CreateUpdateDto(Guid roleId, string roleName)
    {
        return new UpdateRoleDto
        {
            Role = new RoleUpdateInfoDto { Id = roleId, Name = roleName },
        };
    }

    private RoleService CreateService()
    {
        return new RoleService(_unitOfWork.Object, _mapper.Object);
    }
}
