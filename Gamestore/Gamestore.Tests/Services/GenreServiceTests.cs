using System.Linq.Expressions;
using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using Gamestore.Tests.MockData;
using MapsterMapper;
using Moq;

namespace Gamestore.Tests.Services;

public class GenreServiceTests
{
    private readonly Mock<IGenreRepository> _mockGenreRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GenreService _service;

    public GenreServiceTests()
    {
        _mockGenreRepository = new Mock<IGenreRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.Genres)
            .Returns(_mockGenreRepository.Object);

        _service = new GenreService(_mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetAllAsyncShouldReturnAllGenres()
    {
        var genres = GenreMockData.GetGenres();
        var genreDtos = genres.Select(genre => new GenreDto
        {
            Id = genre.Id,
            Name = genre.Name,
            ParentGenreId = genre.ParentGenreId,
        });

        _mockGenreRepository.Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(genres);
        _mockMapper.Setup(mapper => mapper.Map<IEnumerable<GenreDto>>(genres))
            .Returns(genreDtos);

        var result = await _service.GetAllAsync();

        Assert.Equal(4, result.Count());
    }

    [Fact]
    public async Task CreateAsyncShouldCreateGenreWhenNameIsUnique()
    {
        var dto = new CreateGenreDto
        {
            Genre = new GenreCreateInfoDto { Name = "Adventure" },
        };

        _mockGenreRepository.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Genre, bool>>>()))
            .ReturnsAsync((Genre?)null);
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);
        _mockMapper.Setup(mapper => mapper.Map<GenreDto>(It.IsAny<Genre>()))
            .Returns(new GenreDto { Name = "Adventure" });

        var result = await _service.CreateAsync(dto);

        Assert.Equal("Adventure", result.Name);
        _mockGenreRepository.Verify(repository => repository.AddAsync(It.IsAny<Genre>()), Times.Once);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncShouldThrowEntityAlreadyExistsExceptionWhenNameAlreadyExists()
    {
        var dto = new CreateGenreDto
        {
            Genre = new GenreCreateInfoDto { Name = "Action" },
        };

        _mockGenreRepository.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Genre, bool>>>()))
            .ReturnsAsync(new Genre { Name = "Action" });

        await Assert.ThrowsAsync<EntityAlreadyExistsException>(() => _service.CreateAsync(dto));

        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsyncShouldThrowValidationExceptionWhenParentGenreDoesNotExist()
    {
        var parentGenreId = Guid.NewGuid();
        var dto = new CreateGenreDto
        {
            Genre = new GenreCreateInfoDto { Name = "Tactical", ParentGenreId = parentGenreId },
        };

        _mockGenreRepository.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Genre, bool>>>()))
            .ReturnsAsync((Genre?)null);
        _mockGenreRepository.Setup(repository => repository.GetByIdAsync(parentGenreId))
            .ReturnsAsync((Genre?)null);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));

        _mockGenreRepository.Verify(repository => repository.AddAsync(It.IsAny<Genre>()), Times.Never);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsyncShouldThrowNotFoundExceptionWhenGenreDoesNotExist()
    {
        var id = Guid.NewGuid();
        _mockGenreRepository.Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync((Genre?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(id));
    }

    [Fact]
    public async Task UpdateAsyncShouldUpdateGenreWhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var parentGenreId = Guid.NewGuid();
        var genre = new Genre { Id = id, Name = "Action" };
        var dto = new UpdateGenreDto
        {
            Genre = new GenreUpdateInfoDto
            {
                Id = id,
                Name = "Adventure",
                ParentGenreId = parentGenreId,
            },
        };

        _mockGenreRepository.Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync(genre);
        _mockGenreRepository.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Genre, bool>>>()))
            .ReturnsAsync((Genre?)null);
        _mockGenreRepository.Setup(repository => repository.GetByIdAsync(parentGenreId))
            .ReturnsAsync(new Genre { Id = parentGenreId, Name = "Parent" });
        _mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);
        _mockMapper.Setup(mapper => mapper.Map<GenreDto>(genre))
            .Returns(new GenreDto { Id = id, Name = "Adventure", ParentGenreId = parentGenreId });

        var result = await _service.UpdateAsync(dto);

        Assert.Equal("Adventure", result.Name);
        Assert.Equal(parentGenreId, result.ParentGenreId);
        _mockGenreRepository.Verify(repository => repository.Update(genre), Times.Once);
        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsyncShouldThrowNotFoundExceptionWhenGenreDoesNotExist()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateGenreDto
        {
            Genre = new GenreUpdateInfoDto { Id = id, Name = "Adventure" },
        };

        _mockGenreRepository.Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync((Genre?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(dto));

        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldThrowEntityAlreadyExistsExceptionWhenNameAlreadyExists()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateGenreDto
        {
            Genre = new GenreUpdateInfoDto { Id = id, Name = "RPG" },
        };

        _mockGenreRepository.Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync(new Genre { Id = id, Name = "Action" });
        _mockGenreRepository.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Genre, bool>>>()))
            .ReturnsAsync(new Genre { Id = Guid.NewGuid(), Name = "RPG" });

        await Assert.ThrowsAsync<EntityAlreadyExistsException>(() => _service.UpdateAsync(dto));

        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldThrowValidationExceptionWhenGenreIsItsOwnParent()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateGenreDto
        {
            Genre = new GenreUpdateInfoDto { Id = id, Name = "Action", ParentGenreId = id },
        };

        _mockGenreRepository.Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync(new Genre { Id = id, Name = "Action" });
        _mockGenreRepository.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Genre, bool>>>()))
            .ReturnsAsync((Genre?)null);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(dto));

        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncShouldThrowValidationExceptionWhenParentGenreDoesNotExist()
    {
        var id = Guid.NewGuid();
        var parentGenreId = Guid.NewGuid();
        var dto = new UpdateGenreDto
        {
            Genre = new GenreUpdateInfoDto
            {
                Id = id,
                Name = "Action",
                ParentGenreId = parentGenreId,
            },
        };

        _mockGenreRepository.Setup(repository => repository.GetByIdAsync(id))
            .ReturnsAsync(new Genre { Id = id, Name = "Action" });
        _mockGenreRepository.Setup(repository => repository.GetSingleAsync(
                It.IsAny<Expression<Func<Genre, bool>>>()))
            .ReturnsAsync((Genre?)null);
        _mockGenreRepository.Setup(repository => repository.GetByIdAsync(parentGenreId))
            .ReturnsAsync((Genre?)null);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(dto));

        _mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }
}
