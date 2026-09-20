using Gamestore.BLL.DTOs.Publishers;
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
    private readonly Mock<IMapper> _mockMapper;
    private readonly PublisherService _service;

    public PublisherServiceTests()
    {
        _mockPublisherRepository = new Mock<IPublisherRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Publishers)
            .Returns(_mockPublisherRepository.Object);

        _service = new PublisherService(
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetAllAsyncShouldReturnAllPublishers()
    {
        var publishers = new List<Publisher>
        {
            new() { Id = Guid.NewGuid(), CompanyName = "Valve" },
            new() { Id = Guid.NewGuid(), CompanyName = "Ubisoft" },
        };
        var publisherDtos = publishers.Select(publisher => new PublisherDto
        {
            Id = publisher.Id,
            CompanyName = publisher.CompanyName,
        });

        _mockPublisherRepository.Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(publishers);
        _mockMapper.Setup(mapper => mapper.Map<IEnumerable<PublisherDto>>(publishers))
            .Returns(publisherDtos);

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByCompanyNameAsyncShouldReturnPublisherWhenPublisherExists()
    {
        var publisher = new Publisher { Id = Guid.NewGuid(), CompanyName = "Valve" };
        var publisherDto = new PublisherDto { Id = publisher.Id, CompanyName = publisher.CompanyName };

        _mockPublisherRepository.Setup(repository => repository.GetByCompanyNameAsync("Valve"))
            .ReturnsAsync(publisher);
        _mockMapper.Setup(mapper => mapper.Map<PublisherDto>(publisher))
            .Returns(publisherDto);

        var result = await _service.GetByCompanyNameAsync("Valve");

        Assert.Equal(publisher.Id, result.Id);
        Assert.Equal("Valve", result.CompanyName);
    }

    [Fact]
    public async Task GetByGameKeyAsyncShouldReturnPublisherWhenGameExists()
    {
        var publisher = new Publisher { Id = Guid.NewGuid(), CompanyName = "Valve" };
        var publisherDto = new PublisherDto { Id = publisher.Id, CompanyName = publisher.CompanyName };

        _mockPublisherRepository.Setup(repository => repository.GetByGameKeyAsync("half-life"))
            .ReturnsAsync(publisher);
        _mockMapper.Setup(mapper => mapper.Map<PublisherDto>(publisher))
            .Returns(publisherDto);

        var result = await _service.GetByGameKeyAsync("half-life");

        Assert.Equal(publisher.Id, result.Id);
    }

    [Fact]
    public async Task CreateAsyncShouldCreatePublisherWhenCompanyNameIsUnique()
    {
        var dto = new CreatePublisherDto
        {
            Publisher = new PublisherCreateInfoDto
            {
                CompanyName = "Valve",
                HomePage = "https://www.valvesoftware.com",
                Description = "Game publisher",
            },
        };
        Publisher? addedPublisher = null;

        _mockPublisherRepository.Setup(repository => repository.GetByCompanyNameAsync("Valve"))
            .ReturnsAsync((Publisher?)null);
        _mockPublisherRepository.Setup(repository => repository.AddAsync(It.IsAny<Publisher>()))
            .Callback<Publisher>(publisher => addedPublisher = publisher)
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);
        _mockMapper.Setup(mapper => mapper.Map<PublisherDto>(It.IsAny<Publisher>()))
            .Returns((Publisher publisher) => new PublisherDto
            {
                Id = publisher.Id,
                CompanyName = publisher.CompanyName,
                HomePage = publisher.HomePage,
                Description = publisher.Description,
            });

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(addedPublisher);
        Assert.NotEqual(Guid.Empty, addedPublisher.Id);
        Assert.Equal("Valve", result.CompanyName);
        Assert.Equal(dto.Publisher.HomePage, addedPublisher.HomePage);
        Assert.Equal(dto.Publisher.Description, addedPublisher.Description);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncShouldThrowEntityAlreadyExistsExceptionWhenCompanyNameExists()
    {
        var dto = new CreatePublisherDto
        {
            Publisher = new PublisherCreateInfoDto { CompanyName = "Valve" },
        };

        _mockPublisherRepository.Setup(repository => repository.GetByCompanyNameAsync("Valve"))
            .ReturnsAsync(new Publisher { Id = Guid.NewGuid(), CompanyName = "Valve" });

        await Assert.ThrowsAsync<EntityAlreadyExistsException>(() => _service.CreateAsync(dto));

        _mockPublisherRepository.Verify(repository => repository.AddAsync(It.IsAny<Publisher>()), Times.Never);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldUpdatePublisherWhenCompanyNameIsUnique()
    {
        var publisherId = Guid.NewGuid();
        var publisher = new Publisher { Id = publisherId, CompanyName = "Old Name" };
        var dto = new UpdatePublisherDto
        {
            Publisher = new PublisherUpdateInfoDto
            {
                Id = publisherId,
                CompanyName = "Valve",
                HomePage = "https://www.valvesoftware.com",
                Description = "Updated description",
            },
        };

        _mockPublisherRepository.Setup(repository => repository.GetByIdAsync(publisherId))
            .ReturnsAsync(publisher);
        _mockPublisherRepository.Setup(repository => repository.GetByCompanyNameAsync("Valve"))
            .ReturnsAsync((Publisher?)null);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);
        _mockMapper.Setup(mapper => mapper.Map<PublisherDto>(publisher))
            .Returns(() => new PublisherDto
            {
                Id = publisher.Id,
                CompanyName = publisher.CompanyName,
                HomePage = publisher.HomePage,
                Description = publisher.Description,
            });

        var result = await _service.UpdateAsync(dto);

        Assert.Equal("Valve", publisher.CompanyName);
        Assert.Equal(dto.Publisher.HomePage, publisher.HomePage);
        Assert.Equal(dto.Publisher.Description, publisher.Description);
        Assert.Equal("Valve", result.CompanyName);
        _mockPublisherRepository.Verify(repository => repository.Update(publisher), Times.Once);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncShouldThrowNotFoundExceptionWhenPublisherDoesNotExist()
    {
        var publisherId = Guid.NewGuid();

        _mockPublisherRepository.Setup(repository => repository.GetByIdAsync(publisherId))
            .ReturnsAsync((Publisher?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(publisherId));

        _mockPublisherRepository.Verify(
            repository => repository.HasGamesAsync(It.IsAny<Guid>()),
            Times.Never);
        _mockPublisherRepository.Verify(repository => repository.Delete(It.IsAny<Publisher>()), Times.Never);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
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
