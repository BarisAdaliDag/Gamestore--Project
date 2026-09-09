using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Filtering.Abstract;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Filtering.Filters;

public class GenreFilter : IGameFilter
{
    public IQueryable<Game> Apply(IQueryable<Game> query, GameQueryParams parameters)
    {
        var genreIds = parameters.Genres;

        return genreIds?.Count > 0
            ? query.Where(g => g.GameGenres.Any(gg => genreIds.Contains(gg.GenreId)))
            : query;
    }
}