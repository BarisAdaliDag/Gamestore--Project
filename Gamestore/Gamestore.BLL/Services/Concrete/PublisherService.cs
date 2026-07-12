using Gamestore.BLL.DTOs.Publishers;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gamestore.BLL.Services.Concrete;

public class PublisherService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<PublisherService> logger)
    : BaseService<PublisherDto, Publisher>(unitOfWork, mapper), IPublisherService
{
    private readonly ILogger<PublisherService> _logger = logger;

    public PublisherService(IUnitOfWork unitOfWork, IMapper mapper)
        : this(unitOfWork, mapper, NullLogger<PublisherService>.Instance)
    {
    }

    protected override IRepository<Publisher> Repository => UnitOfWork.Publishers;

    public async Task<PublisherDto> GetByCompanyNameAsync(string companyName)
    {
        var publisher = await UnitOfWork.Publishers.GetByCompanyNameAsync(companyName)
            ?? throw new NotFoundException(nameof(Publisher), companyName);

        return Mapper.Map<PublisherDto>(publisher);
    }

    public async Task<PublisherDto> GetByGameKeyAsync(string gameKey)
    {
        var publisher = await UnitOfWork.Publishers.GetByGameKeyAsync(gameKey)
            ?? throw new NotFoundException($"Publisher for game with key '{gameKey}' was not found.");

        return Mapper.Map<PublisherDto>(publisher);
    }

    public async Task<PublisherDto> CreateAsync(CreatePublisherDto dto)
    {
        _logger.LogInformation("Creating publisher: {CompanyName}", dto.Publisher.CompanyName);

        var existing = await UnitOfWork.Publishers.GetByCompanyNameAsync(dto.Publisher.CompanyName);
        if (existing != null)
        {
            throw new EntityAlreadyExistsException(
                nameof(Publisher),
                "company name",
                dto.Publisher.CompanyName);
        }

        var publisher = new Publisher
        {
            Id = Guid.NewGuid(),
            CompanyName = dto.Publisher.CompanyName,
            HomePage = dto.Publisher.HomePage,
            Description = dto.Publisher.Description,
        };

        await UnitOfWork.Publishers.AddAsync(publisher);
        await UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Publisher created: {CompanyName} ({Id})", publisher.CompanyName, publisher.Id);

        return Mapper.Map<PublisherDto>(publisher);
    }

    public async Task<PublisherDto> UpdateAsync(UpdatePublisherDto dto)
    {
        _logger.LogInformation("Updating publisher: {Id}", dto.Publisher.Id);

        var publisher = await UnitOfWork.Publishers.GetByIdAsync(dto.Publisher.Id)
            ?? throw new NotFoundException(nameof(Publisher), dto.Publisher.Id);

        var nameOwner = await UnitOfWork.Publishers.GetByCompanyNameAsync(dto.Publisher.CompanyName);
        if (nameOwner != null && nameOwner.Id != dto.Publisher.Id)
        {
            throw new EntityAlreadyExistsException(
                nameof(Publisher),
                "company name",
                dto.Publisher.CompanyName);
        }

        publisher.CompanyName = dto.Publisher.CompanyName;
        publisher.HomePage = dto.Publisher.HomePage;
        publisher.Description = dto.Publisher.Description;

        UnitOfWork.Publishers.Update(publisher);
        await UnitOfWork.SaveChangesAsync();

        _logger.LogInformation("Publisher updated: {CompanyName} ({Id})", publisher.CompanyName, publisher.Id);

        return Mapper.Map<PublisherDto>(publisher);
    }
}
