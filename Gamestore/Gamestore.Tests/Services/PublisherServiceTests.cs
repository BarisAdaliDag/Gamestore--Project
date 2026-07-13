using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Moq;

namespace Gamestore.Tests.Services;

public class PublisherServiceTests
{
    private readonly Mock<IPublisherRepository> _mockPublisherRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly PublisherService _service;

    public PublisherServiceTests()
    {
        _mockPublisherRepository = new Mock<IPublisherRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Publishers)
            .Returns(_mockPublisherRepository.Object);

        _service = new PublisherService(
            _mockUnitOfWork.Object,
            Mock.Of<IMapper>());
    }

    [Fact]
    public async Task DeleteAsyncShouldThrowEntityInUseExceptionWhenPublisherHasGames()
    {
        var publisherId = Guid.NewGuid();
        var publisher = new Publisher { Id = publisherId, CompanyName = "Valve" };

        _mockPublisherRepository.Setup(repository => repository.GetByIdAsync(publisherId))
            .ReturnsAsync(publisher);
        _mockPublisherRepository.Setup(repository => repository.HasGamesAsync(publisherId))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<EntityInUseException>(() => _service.DeleteAsync(publisherId));

        _mockPublisherRepository.Verify(repository => repository.Delete(It.IsAny<Publisher>()), Times.Never);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsyncShouldDeletePublisherWhenPublisherHasNoGames()
    {
        var publisherId = Guid.NewGuid();
        var publisher = new Publisher { Id = publisherId, CompanyName = "Valve" };

        _mockPublisherRepository.Setup(repository => repository.GetByIdAsync(publisherId))
            .ReturnsAsync(publisher);
        _mockPublisherRepository.Setup(repository => repository.HasGamesAsync(publisherId))
            .ReturnsAsync(false);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        await _service.DeleteAsync(publisherId);

        _mockPublisherRepository.Verify(repository => repository.Delete(publisher), Times.Once);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }
}
