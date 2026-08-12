using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Filtering.Abstract;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Filtering.Filters;

public class NameFilter : IGameFilter
{
    private const int MinLength = 3;

    public IQueryable<Game> Apply(IQueryable<Game> query, GameQueryParams parameters)
    {
        var name = parameters.Name?.Trim();

        // Shorter than 3 chars => filtering does NOT start (not an error)
        return name?.Length >= MinLength
            ? query.Where(g => g.Name.Contains(name))
            : query;
    }
}