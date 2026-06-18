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

    [Fact]
    public async Task CreateAsyncShouldCreateGameAndCallSaveChanges()
    {
        var genreId = Guid.NewGuid();
        var platformId = Guid.NewGuid();
        var dto = new CreateGameDto
        {
            Game = new GameCreateInfoDto { Name = "New Game", Key = "new-game" },
            Genres = [genreId],
            Platforms = [platformId],
        };

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByKeyAsync("new-game"))
            .ReturnsAsync((Game?)null);

        var mockGenreRepository = new Mock<IGenreRepository>();
        mockGenreRepository.Setup(repository => repository.GetByIdAsync(genreId))
            .ReturnsAsync(new Genre { Id = genreId, Name = "Action" });

        var mockPlatformRepository = new Mock<IPlatformRepository>();
        mockPlatformRepository.Setup(repository => repository.GetByIdAsync(platformId))
            .ReturnsAsync(new Platform { Id = platformId, Type = "PC" });

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Genres)
            .Returns(mockGenreRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Platforms)
            .Returns(mockPlatformRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        var mockMapper = new Mock<IMapper>();
        mockMapper.Setup(mapper => mapper.Map<GameDto>(It.IsAny<Game>()))
            .Returns(new GameDto { Name = "New Game", Key = "new-game" });

        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object);

        var result = await service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("new-game", result.Key);
        mockGameRepository.Verify(repository => repository.AddAsync(It.IsAny<Game>()), Times.Once);
        mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncShouldThrowEntityAlreadyExistsExceptionWhenKeyAlreadyExists()
    {
        var dto = new CreateGameDto
        {
            Game = new GameCreateInfoDto { Name = "Existing", Key = "existing-game" },
            Genres = [],
            Platforms = [],
        };

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByKeyAsync("existing-game"))
            .ReturnsAsync(new Game { Name = "Existing", Key = "existing-game" });

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);

        var mockMapper = new Mock<IMapper>();
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object);

        await Assert.ThrowsAsync<EntityAlreadyExistsException>(() => service.CreateAsync(dto));

        mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }
}
