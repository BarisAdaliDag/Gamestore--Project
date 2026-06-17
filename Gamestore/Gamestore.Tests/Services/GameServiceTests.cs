using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
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
}
