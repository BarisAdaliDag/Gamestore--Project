using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Services.Abstract;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Moq;

namespace Gamestore.Tests.Services;

public class UnifiedGameCatalogServiceTests
{
    [Fact]
    public async Task GetGamesAsyncShouldPreferSqlGameWhenNameIsDuplicated()
    {
        var sqlGame = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Chai",
            Key = "sql-chai",
        };
        var mongoItem = CreateMongoItem("Chai", "mongo-chai", viewCount: 10);
        var service = CreateService(sqlGame, mongoItem);

        var result = await service.GetGamesAsync(new GameQueryParams { PageCount = "all" });

        var game = Assert.Single(result.Games);
        Assert.Equal("sql-chai", game.Key);
    }

    [Fact]
    public async Task GetGamesAsyncShouldUseMongoViewCountForPopularSorting()
    {
        var sqlGame = new Game
        {
            Id = Guid.NewGuid(),
            Name = "SQL Game",
            Key = "sql-game",
            ViewCount = 1,
        };
        var mongoItem = CreateMongoItem("Mongo Game", "mongo-game", viewCount: 10);
        var service = CreateService(sqlGame, mongoItem);

        var result = await service.GetGamesAsync(new GameQueryParams
        {
            PageCount = "all",
            Sort = "most popular",
        });

        Assert.Equal("mongo-game", result.Games.First().Key);
    }

    [Fact]
    public async Task GetGamesAsyncShouldHideDeletedNorthwindGame()
    {
        const string deletedKey = "mongo-deleted-game";
        var sqlGame = new Game
        {
            Id = Guid.NewGuid(),
            Name = "SQL Game",
            Key = "sql-game",
        };
        var mongoItem = CreateMongoItem("Deleted Mongo Game", deletedKey, viewCount: 0);
        var service = CreateService(sqlGame, mongoItem, [deletedKey]);

        var result = await service.GetGamesAsync(new GameQueryParams { PageCount = "all" });

        Assert.DoesNotContain(result.Games, game => game.Key == deletedKey);
    }

    private static UnifiedGameCatalogService CreateService(
        Game sqlGame,
        NorthwindGameCatalogItem mongoItem,
        IReadOnlyCollection<string>? deletedNorthwindKeys = null)
    {
        var games = new Mock<IGameRepository>();
        games.Setup(repository => repository.GetAllWithDetailsAsync())
            .ReturnsAsync([sqlGame]);
        games.Setup(repository => repository.GetDeletedNorthwindKeysAsync())
            .ReturnsAsync(deletedNorthwindKeys ?? []);
        var publishers = new Mock<IPublisherRepository>();
        publishers.Setup(repository => repository.GetAllAsync())
            .ReturnsAsync([]);
        var genres = new Mock<IGenreRepository>();
        genres.Setup(repository => repository.GetAllAsync())
            .ReturnsAsync([]);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.Games).Returns(games.Object);
        unitOfWork.Setup(work => work.Publishers).Returns(publishers.Object);
        unitOfWork.Setup(work => work.Genres).Returns(genres.Object);
        var mapper = new Mock<IMapper>();
        mapper.Setup(instance => instance.Map<GameDto>(sqlGame))
            .Returns(new GameDto
            {
                Id = sqlGame.Id,
                Name = sqlGame.Name,
                Key = sqlGame.Key,
            });
        var northwindProducts = new Mock<INorthwindProductService>();
        northwindProducts.Setup(service => service.GetAllWithReferencesAsync())
            .ReturnsAsync([mongoItem]);

        return new UnifiedGameCatalogService(
            unitOfWork.Object,
            mapper.Object,
            northwindProducts.Object);
    }

    private static NorthwindGameCatalogItem CreateMongoItem(
        string name,
        string key,
        int viewCount)
    {
        return new NorthwindGameCatalogItem
        {
            Game = new GameDto
            {
                Id = Guid.NewGuid(),
                Name = name,
                Key = key,
            },
            ViewCount = viewCount,
        };
    }
}
