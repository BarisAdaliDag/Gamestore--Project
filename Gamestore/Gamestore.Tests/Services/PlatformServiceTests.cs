using System.Linq.Expressions;
using Gamestore.BLL.DTOs.Platforms;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using Gamestore.Tests.MockData;
using MapsterMapper;
using Moq;

namespace Gamestore.Tests.Services;

public class PlatformServiceTests
{
    private readonly Mock<IPlatformRepository> _mockPlatformRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PlatformService _service;

    public PlatformServiceTests()
    {
        _mockPlatformRepository = new Mock<IPlatformRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Platforms)
            .Returns(_mockPlatformRepository.Object);

        _service = new PlatformService(_mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetAllAsyncShouldReturnAllPlatforms()
    {
        var platforms = PlatformMockData.GetPlatforms();
        var platformDtos = platforms.Select(platform => new PlatformDto
        {
            Id = platform.Id,
            Type = platform.Type,
        });

        _mockPlatformRepository.Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(platforms);
        _mockMapper.Setup(mapper => mapper.Map<IEnumerable<PlatformDto>>(platforms))
            .Returns(platformDtos);

        var result = await _service.GetAllAsync();

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task CreateAsyncShouldCreatePlatformWhenTypeIsUnique()
    {
        var dto = new CreatePlatformDto
        {
            Platform = new PlatformCreateInfoDto { Type = "VR" },
        };

        _mockPlatformRepository.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Platform, bool>>>()))
            .ReturnsAsync((Platform?)null);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);
        _mockMapper.Setup(mapper => mapper.Map<PlatformDto>(It.IsAny<Platform>()))
            .Returns(new PlatformDto { Type = "VR" });

        var result = await _service.CreateAsync(dto);

        Assert.Equal("VR", result.Type);
        _mockPlatformRepository.Verify(repository => repository.AddAsync(It.IsAny<Platform>()), Times.Once);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncShouldThrowEntityAlreadyExistsExceptionWhenTypeAlreadyExists()
    {
        var dto = new CreatePlatformDto
        {
            Platform = new PlatformCreateInfoDto { Type = "Mobile" },
        };

        _mockPlatformRepository.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Platform, bool>>>()))
            .ReturnsAsync(new Platform { Type = "Mobile" });

        await Assert.ThrowsAsync<EntityAlreadyExistsException>(() => _service.CreateAsync(dto));

        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsyncShouldThrowNotFoundExceptionWhenPlatformDoesNotExist()
    {
        var id = Guid.NewGuid();
        _mockPlatformRepository.Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync((Platform?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(id));
    }
}
