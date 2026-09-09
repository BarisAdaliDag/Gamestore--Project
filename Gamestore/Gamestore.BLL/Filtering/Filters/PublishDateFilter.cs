using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Filtering.Abstract;
using Gamestore.BLL.Helpers;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Filtering.Filters;

public class PublishDateFilter(TimeProvider timeProvider) : IGameFilter
{
    public IQueryable<Game> Apply(IQueryable<Game> query, GameQueryParams parameters)
    {
        var cutoff = PublishDateOptionsHelper.ResolveCutoff(
            parameters.DatePublishing,
            timeProvider.GetUtcNow().UtcDateTime);

        return cutoff.HasValue
            ? query.Where(g => g.CreatedAt >= cutoff.Value)
            : query;
    }
}