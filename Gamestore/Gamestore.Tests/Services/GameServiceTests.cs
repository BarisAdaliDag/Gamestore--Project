using System.Linq.Expressions;
using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Filtering;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Moq;

namespace Gamestore.Tests.Services;

public class GameServiceTests
{
    private static readonly GameQueryPipeline EmptyPipeline = new([]);

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
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

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
        mockGameRepository.Setup(repository => repository.IncrementViewCountAsync(game.Id))
            .Returns(Task.CompletedTask);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);

        var mockMapper = new Mock<IMapper>();
        mockMapper.Setup(mapper => mapper.Map<GameDto>(game))
            .Returns(gameDto);

        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

        var result = await service.GetByKeyAsync(key);

        Assert.NotNull(result);
        Assert.Equal(key, result.Key);
        Assert.Equal("Halo", result.Name);
        mockGameRepository.Verify(
            repository => repository.IncrementViewCountAsync(game.Id),
            Times.Once);
    }

    [Fact]
    public async Task GetByKeyAsyncShouldNotIncrementViewCountWhenGameDoesNotExist()
    {
        var key = "non-existing-game";

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByKeyAsync(key))
            .ReturnsAsync((Game?)null);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);

        var mockMapper = new Mock<IMapper>();
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByKeyAsync(key));

        mockGameRepository.Verify(
            repository => repository.IncrementViewCountAsync(It.IsAny<Guid>()),
            Times.Never);
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
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByKeyAsync(key));
    }

    [Fact]
    public async Task GetByPublisherNameAsyncShouldReturnMappedGames()
    {
        const string CompanyName = "Valve";
        var games = new List<Game>
        {
            new() { Id = Guid.NewGuid(), Name = "Half-Life", Key = "half-life" },
            new() { Id = Guid.NewGuid(), Name = "Portal", Key = "portal" },
        };
        var gameDtos = games.Select(game => new GameDto
        {
            Id = game.Id,
            Name = game.Name,
            Key = game.Key,
        });
        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByPublisherNameAsync(CompanyName))
            .ReturnsAsync(games);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);
        var mockMapper = new Mock<IMapper>();
        mockMapper.Setup(mapper => mapper.Map<IEnumerable<GameDto>>(games))
            .Returns(gameDtos);
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

        var result = await service.GetByPublisherNameAsync(CompanyName);

        Assert.Equal(2, result.Count());
        mockGameRepository.Verify(
            repository => repository.GetByPublisherNameAsync(CompanyName),
            Times.Once);
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
        Game? addedGame = null;
        mockGameRepository.Setup(repository => repository.AddAsync(It.IsAny<Game>()))
            .Callback<Game>(game => addedGame = game)
            .Returns(Task.CompletedTask);

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

        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

        var result = await service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.NotNull(addedGame);
        Assert.Equal("new-game", result.Key);
        Assert.Equal(dto.Game.Price, addedGame.Price);
        Assert.Equal(dto.Game.UnitInStock, addedGame.UnitInStock);
        Assert.Equal(dto.Game.Discount, addedGame.Discount);
        Assert.Equal(publisherId, addedGame.PublisherId);
        mockGameRepository.Verify(repository => repository.AddAsync(It.IsAny<Game>()), Times.Once);
        mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncShouldThrowValidationExceptionWhenPublisherDoesNotExist()
    {
        var genreId = Guid.NewGuid();
        var platformId = Guid.NewGuid();
        var publisherId = Guid.NewGuid();
        var dto = new CreateGameDto
        {
            Game = new GameCreateInfoDto { Name = "New Game", Key = "new-game" },
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
            .ReturnsAsync((Publisher?)null);
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games).Returns(mockGameRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Genres).Returns(mockGenreRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Platforms).Returns(mockPlatformRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Publishers).Returns(mockPublisherRepository.Object);
        var service = new GameService(mockUnitOfWork.Object, Mock.Of<IMapper>(), EmptyPipeline);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(dto));

        mockGameRepository.Verify(repository => repository.AddAsync(It.IsAny<Game>()), Times.Never);
        mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldUpdateEpicFourFieldsAndPublisher()
    {
        var gameId = Guid.NewGuid();
        var genreId = Guid.NewGuid();
        var platformId = Guid.NewGuid();
        var publisherId = Guid.NewGuid();
        var game = new Game { Id = gameId, Name = "Old Name", Key = "old-key" };
        var dto = new UpdateGameDto
        {
            Game = new GameUpdateInfoDto
            {
                Id = gameId,
                Name = "Updated Game",
                Key = "updated-game",
                Price = 49.99,
                UnitInStock = 25,
                Discount = 10,
            },
            Genres = [genreId],
            Platforms = [platformId],
            Publisher = publisherId,
        };
        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByIdWithDetailsAsync(gameId))
            .ReturnsAsync(game);
        mockGameRepository.Setup(repository => repository.GetByKeyAsync("updated-game"))
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
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games).Returns(mockGameRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Genres).Returns(mockGenreRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Platforms).Returns(mockPlatformRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Publishers).Returns(mockPublisherRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).ReturnsAsync(1);
        var mockMapper = new Mock<IMapper>();
        mockMapper.Setup(mapper => mapper.Map<GameDto>(game))
            .Returns(new GameDto { Id = gameId, Name = dto.Game.Name, Key = dto.Game.Key });
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

        await service.UpdateAsync(dto);

        Assert.Equal(dto.Game.Price, game.Price);
        Assert.Equal(dto.Game.UnitInStock, game.UnitInStock);
        Assert.Equal(dto.Game.Discount, game.Discount);
        Assert.Equal(publisherId, game.PublisherId);
        Assert.Contains(game.GameGenres, relation => relation.GenreId == genreId);
        Assert.Contains(game.GamePlatforms, relation => relation.PlatformId == platformId);
        mockGameRepository.Verify(repository => repository.Update(game), Times.Once);
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
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

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

        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

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

        List<OrderGame> noOrderGames = [];
        var mockOrderGameRepository = new Mock<IRepository<OrderGame>>();
        mockOrderGameRepository.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<OrderGame, bool>>>()))
            .ReturnsAsync(noOrderGames);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.OrderGames)
            .Returns(mockOrderGameRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        var mockMapper = new Mock<IMapper>();
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

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
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

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

        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

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
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GenerateGameFileAsync(key));
    }

    [Fact]
    public async Task DeleteByKeyAsyncShouldThrowEntityInUseExceptionWhenGameIsInAnOrder()
    {
        var key = "halo";
        var game = new Game { Id = Guid.NewGuid(), Name = "Halo", Key = key };

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByKeyAsync(key))
            .ReturnsAsync(game);

        List<OrderGame> orderGames = [new() { OrderId = Guid.NewGuid(), ProductId = game.Id }];
        var mockOrderGameRepository = new Mock<IRepository<OrderGame>>();
        mockOrderGameRepository.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<OrderGame, bool>>>()))
            .ReturnsAsync(orderGames);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.OrderGames)
            .Returns(mockOrderGameRepository.Object);

        var mockMapper = new Mock<IMapper>();
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

        await Assert.ThrowsAsync<EntityInUseException>(() => service.DeleteByKeyAsync(key));

        mockGameRepository.Verify(repository => repository.Delete(It.IsAny<Game>()), Times.Never);
        mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteByKeyAsyncShouldDeleteGameWhenGameIsNotInAnyOrder()
    {
        var key = "halo";
        var game = new Game { Id = Guid.NewGuid(), Name = "Halo", Key = key };

        var mockGameRepository = new Mock<IGameRepository>();
        mockGameRepository.Setup(repository => repository.GetByKeyAsync(key))
            .ReturnsAsync(game);

        List<OrderGame> noOrderGames = [];
        var mockOrderGameRepository = new Mock<IRepository<OrderGame>>();
        mockOrderGameRepository.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<OrderGame, bool>>>()))
            .ReturnsAsync(noOrderGames);

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.Games)
            .Returns(mockGameRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.OrderGames)
            .Returns(mockOrderGameRepository.Object);
        mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        var mockMapper = new Mock<IMapper>();
        var service = new GameService(mockUnitOfWork.Object, mockMapper.Object, EmptyPipeline);

        await service.DeleteByKeyAsync(key);

        mockGameRepository.Verify(repository => repository.Delete(game), Times.Once);
        mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }
}