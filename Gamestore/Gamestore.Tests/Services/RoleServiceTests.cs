using System.Linq.Expressions;
using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Moq;

namespace Gamestore.Tests.Services;

public class RoleServiceTests
{
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IRepository<Role>> _roles = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public RoleServiceTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Roles).Returns(_roles.Object);
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

    private RoleService CreateService()
    {
        return new RoleService(_unitOfWork.Object, _mapper.Object);
    }
}
