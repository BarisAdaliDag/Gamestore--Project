using Gamestore.BLL.DTOs.Publishers;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Helpers;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Mongo.Documents;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using Gamestore.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gamestore.BLL.Services.Concrete;

public class PublisherService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<PublisherService> logger,
    INorthwindSupplierRepository? supplierRepository = null,
    IGameMaterializationService? gameMaterializationService = null)
    : BaseService<PublisherDto, Publisher>(unitOfWork, mapper), IPublisherService
{
    private readonly ILogger<PublisherService> _logger = logger;
    private readonly INorthwindSupplierRepository? _supplierRepository = supplierRepository;
    private readonly IGameMaterializationService? _gameMaterializationService = gameMaterializationService;

    public PublisherService(IUnitOfWork unitOfWork, IMapper mapper)
        : this(unitOfWork, mapper, NullLogger<PublisherService>.Instance)
    {
    }

    protected override IRepository<Publisher> Repository => UnitOfWork.Publishers;

    public async Task<PublisherDto> GetByCompanyNameAsync(string companyName)
    {
        var publisher = await UnitOfWork.Publishers.GetByCompanyNameAsync(companyName);
        if (publisher == null && _supplierRepository != null)
        {
            var supplier = FindNorthwindSupplierByName(companyName);
            publisher = supplier == null
                ? null
                : await MaterializeSupplierAsync(supplier);
        }

        return Mapper.Map<PublisherDto>(
            publisher ?? throw new NotFoundException(nameof(Publisher), companyName));
    }

    public async Task<PublisherDto> GetByGameKeyAsync(string gameKey)
    {
        var publisher = await UnitOfWork.Publishers.GetByGameKeyAsync(gameKey);
        if (publisher == null && _gameMaterializationService != null)
        {
            await _gameMaterializationService.GetOrCreateAsync(gameKey);
            publisher = await UnitOfWork.Publishers.GetByGameKeyAsync(gameKey);
        }

        return Mapper.Map<PublisherDto>(
            publisher ?? throw new NotFoundException(
                $"Publisher for game with key '{gameKey}' was not found."));
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
            ?? await MaterializeNorthwindSupplierByIdAsync(dto.Publisher.Id)
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

    public override async Task DeleteAsync(Guid id)
    {
        var publisher = await UnitOfWork.Publishers.GetByIdAsync(id)
            ?? await MaterializeNorthwindSupplierByIdAsync(id)
            ?? throw new NotFoundException(nameof(Publisher), id);

        if (await UnitOfWork.Publishers.HasGamesAsync(id))
        {
            throw new EntityInUseException(nameof(Publisher), id);
        }

        UnitOfWork.Publishers.Delete(publisher);
        await UnitOfWork.SaveChangesAsync();
    }

    private NorthwindSupplierDocument? FindNorthwindSupplierByName(string companyName)
    {
        return _supplierRepository?
            .Query()
            .AsEnumerable()
            .Where(supplier => !string.IsNullOrWhiteSpace(supplier.CompanyName))
            .GroupBy(supplier => NorthwindReferenceIdentity.Normalize(supplier.CompanyName!))
            .Select(group => group.First())
            .FirstOrDefault(supplier => string.Equals(
                NorthwindReferenceIdentity.Normalize(supplier.CompanyName!),
                NorthwindReferenceIdentity.Normalize(companyName),
                StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Publisher?> MaterializeNorthwindSupplierByIdAsync(Guid id)
    {
        var supplier = _supplierRepository?
            .Query()
            .AsEnumerable()
            .Where(item => !string.IsNullOrWhiteSpace(item.CompanyName))
            .FirstOrDefault(item => NorthwindReferenceIdentity.CreateSupplierId(item.CompanyName!) == id);

        return supplier == null
            ? null
            : await MaterializeSupplierAsync(supplier);
    }

    private async Task<Publisher> MaterializeSupplierAsync(NorthwindSupplierDocument supplier)
    {
        var existing = await UnitOfWork.Publishers.GetByCompanyNameAsync(supplier.CompanyName!);
        if (existing != null)
        {
            return existing;
        }

        var publisher = new Publisher
        {
            Id = NorthwindReferenceIdentity.CreateSupplierId(supplier.CompanyName!),
            CompanyName = supplier.CompanyName!,
            HomePage = supplier.HomePage,
            Description = supplier.ContactName,
        };

        await UnitOfWork.Publishers.AddAsync(publisher);
        await UnitOfWork.SaveChangesAsync();
        return publisher;
    }
}
