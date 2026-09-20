using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Filtering.Abstract;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Filtering.Filters;

public class PublisherFilter : IGameFilter
{
    public IQueryable<Game> Apply(IQueryable<Game> query, GameQueryParams parameters)
    {
        var publisherIds = parameters.Publishers;

        return publisherIds?.Count > 0
            ? query.Where(g => publisherIds.Contains(g.PublisherId))
            : query;
    }
}