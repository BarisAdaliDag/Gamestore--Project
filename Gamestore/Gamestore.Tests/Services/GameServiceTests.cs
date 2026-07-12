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
        var publisherId = Guid.NewGuid();
        var dto = new CreateGameDto
        {
            Game = new GameCreateInfoDto
            {
                Name = "New Game",
                Key = "new-game",
                Price = 10,
                UnitInStock = 5,
                Discount = 0,
            },
            Genres = [genreId],
            Platforms = [platformId],
            Publisher = publisherId,
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

        var mockPublisherRepository = new Mock<IPublisherRepository>();
        mockPublisherRepository.Setup(repository => repository.GetByIdAsync(publisherId))
            .ReturnsAsync(new Publisher { Id = publisherId, CompanyName = "Valve" });

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Genres)
            .Returns(mockGenreRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Platforms)
            .Returns(mockPlatformRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Publishers)
            .Returns(mockPublisherRepository.Object);
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

    [Fact]
    public async Task GetByGenreIdAsyncShouldReturnMappedGames()
    {
        var genreId = Guid.NewGuid();
        var games = new List<Game>
        {
            new() { Id = Guid.NewGuid(), Name = "Halo", Key = "halo" },
            new() { Id = Guid.NewGuid(), Name = "Doom", Key = "doom" },
        };
        var gameDtos = games.Select(game => new GameDto
        {
            Id = game.Id,
            Name = game.Name,
            Key = game.Key,
        });

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByGenreIdAsync(genreId))
            .ReturnsAsync(games);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);

        var mockMapper = new Mock<IMapper>();
        mockMapper.Setup(mapper => mapper.Map<IEnumerable<GameDto>>(games))
            .Returns(gameDtos);

        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object);

        var result = await service.GetByGenreIdAsync(genreId);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task DeleteByKeyAsyncShouldDeleteGameAndCallSaveChanges()
    {
        var key = "halo";
        var game = new Game { Id = Guid.NewGuid(), Name = "Halo", Key = key };

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByKeyAsync(key))
            .ReturnsAsync(game);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        var mockMapper = new Mock<IMapper>();
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object);

        await service.DeleteByKeyAsync(key);

        mockGameRepository.Verify(repository => repository.Delete(game), Times.Once);
        mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteByKeyAsyncShouldThrowNotFoundExceptionWhenGameDoesNotExist()
    {
        var key = "missing-game";

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByKeyAsync(key))
            .ReturnsAsync((Game?)null);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);

        var mockMapper = new Mock<IMapper>();
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteByKeyAsync(key));

        mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GenerateGameFileAsyncShouldReturnTextFileWhenGameExists()
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

        var (fileBytes, fileName) = await service.GenerateGameFileAsync(key);

        Assert.NotEmpty(fileBytes);
        Assert.StartsWith("Halo_", fileName, StringComparison.Ordinal);
        Assert.EndsWith(".txt", fileName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateGameFileAsyncShouldThrowNotFoundExceptionWhenGameDoesNotExist()
    {
        var key = "missing-game";

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByKeyAsync(key))
            .ReturnsAsync((Game?)null);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);

        var mockMapper = new Mock<IMapper>();
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GenerateGameFileAsync(key));
    }
}
