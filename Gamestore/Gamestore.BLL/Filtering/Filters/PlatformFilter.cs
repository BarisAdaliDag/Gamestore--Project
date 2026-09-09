using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Filtering.Abstract;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Filtering.Filters;

public class PlatformFilter : IGameFilter
{
    public IQueryable<Game> Apply(IQueryable<Game> query, GameQueryParams parameters)
    {
        var platformIds = parameters.Platforms;

        return platformIds?.Count > 0
            ? query.Where(g => g.GamePlatforms.Any(gp => platformIds.Contains(gp.PlatformId)))
            : query;
    }
}