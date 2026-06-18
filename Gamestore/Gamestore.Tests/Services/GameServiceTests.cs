using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Moq;

namespace Gamestore.Tests.Services;

public class GameServiceTests
{
    [Fact]
    public async Task GetTotalCountAsyncShouldReturnCountFromRepository()
    {
        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.CountAsync())
            .ReturnsAsync(5);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);

        var mockMapper = new Mock<IMapper>();
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object);

        var result = await service.GetTotalCountAsync();

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task GetByKeyAsyncShouldReturnGameDtoWhenGameExists()
    {
        var key = "halo";
        var game = new Game { Id = Guid.NewGuid(), Name = "Halo", Key = key };
        var gameDto = new GameDto { Id = game.Id, Name = "Halo", Key = key };

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByKeyAsync(key))
            .ReturnsAsync(game);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);

        var mockMapper = new Mock<IMapper>();
        mockMapper.Setup(mapper => mapper.Map<GameDto>(game))
            .Returns(gameDto);

        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object);

        var result = await service.GetByKeyAsync(key);

        Assert.NotNull(result);
        Assert.Equal(key, result.Key);
        Assert.Equal("Halo", result.Name);
    }

    [Fact]
    public async Task GetByKeyAsyncShouldThrowNotFoundExceptionWhenGameDoesNotExist()
    {
        var key = "non-existing-game";

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByKeyAsync(key))
            .ReturnsAsync((Game?)null);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);

        var mockMapper = new Mock<IMapper>();
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByKeyAsync(key));
    }
}
