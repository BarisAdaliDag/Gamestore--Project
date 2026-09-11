using Gamestore.BLL.Services.Abstract;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using Moq;

namespace Gamestore.Tests.Services;

public class GameMaterializationServiceTests
{
    [Fact]
    public async Task GetOrCreateAsyncShouldKeepExistingSqlCopy()
    {
        const string gameKey = "mongo-507f1f77bcf86cd799439011";
        var editedGame = new Game
        {
            Id = Guid.NewGuid(),
            Name = "User Edited Name",
            Key = gameKey,
            Price = 99,
        };
        var gameRepository = new Mock<IGameRepository>();
        gameRepository.Setup(repository => repository.GetByKeyWithDetailsAsync(gameKey))
            .ReturnsAsync(editedGame);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.Games).Returns(gameRepository.Object);
        var northwindProducts = new Mock<INorthwindProductService>();
        var service = new GameMaterializationService(
            unitOfWork.Object,
            northwindProducts.Object);

        var result = await service.GetOrCreateAsync(gameKey);

        Assert.Same(editedGame, result);
        Assert.Equal("User Edited Name", result!.Name);
        northwindProducts.Verify(
            products => products.GetCatalogItemByKeyAsync(It.IsAny<string>()),
            Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(), Times.Never);
    }
}
