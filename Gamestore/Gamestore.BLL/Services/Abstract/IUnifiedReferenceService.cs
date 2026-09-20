using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.DTOs.Publishers;

namespace Gamestore.BLL.Services.Abstract;

public interface IUnifiedReferenceService
{
    Task<IReadOnlyList<PublisherDto>> GetPublishersAsync();

    Task<IReadOnlyList<GenreDto>> GetGenresAsync();
}
