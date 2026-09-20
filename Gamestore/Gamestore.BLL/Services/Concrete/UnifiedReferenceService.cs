using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.DTOs.Publishers;
using Gamestore.BLL.Helpers;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using Gamestore.DAL.UnitOfWork;
using MapsterMapper;

namespace Gamestore.BLL.Services.Concrete;

public sealed class UnifiedReferenceService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    INorthwindSupplierRepository supplierRepository,
    INorthwindCategoryRepository categoryRepository) : IUnifiedReferenceService
{
    public async Task<IReadOnlyList<PublisherDto>> GetPublishersAsync()
    {
        var publishers = mapper.Map<IEnumerable<PublisherDto>>(
                await unitOfWork.Publishers.GetAllAsync())
            .ToList();

        var names = publishers
            .Select(publisher => NorthwindReferenceIdentity.Normalize(publisher.CompanyName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mongoSuppliers = supplierRepository
            .Query()
            .AsEnumerable()
            .Where(supplier => !string.IsNullOrWhiteSpace(supplier.CompanyName))
            .GroupBy(supplier => NorthwindReferenceIdentity.Normalize(supplier.CompanyName!))
            .Select(group => group.First())
            .Where(supplier => names.Add(
                NorthwindReferenceIdentity.Normalize(supplier.CompanyName!)))
            .Select(supplier => new PublisherDto
            {
                Id = NorthwindReferenceIdentity.CreateSupplierId(supplier.CompanyName!),
                CompanyName = supplier.CompanyName!,
                HomePage = supplier.HomePage,
                Description = supplier.ContactName,
            });

        publishers.AddRange(mongoSuppliers);
        return publishers;
    }

    public async Task<IReadOnlyList<GenreDto>> GetGenresAsync()
    {
        var genres = mapper.Map<IEnumerable<GenreDto>>(
                await unitOfWork.Genres.GetAllAsync())
            .ToList();

        var names = genres
            .Select(genre => NorthwindReferenceIdentity.Normalize(genre.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mongoCategories = categoryRepository
            .Query()
            .AsEnumerable()
            .Where(category => !string.IsNullOrWhiteSpace(category.CategoryName))
            .GroupBy(category => NorthwindReferenceIdentity.Normalize(category.CategoryName!))
            .Select(group => group.First())
            .Where(category => names.Add(
                NorthwindReferenceIdentity.Normalize(category.CategoryName!)))
            .Select(category => new GenreDto
            {
                Id = NorthwindReferenceIdentity.CreateCategoryId(category.CategoryName!),
                Name = category.CategoryName!,
                ParentGenreId = null,
            });

        genres.AddRange(mongoCategories);
        return genres;
    }
}
