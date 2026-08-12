using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Filtering.Abstract;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Filtering.Filters;

public class PriceRangeFilter : IGameFilter
{
    public IQueryable<Game> Apply(IQueryable<Game> query, GameQueryParams parameters)
    {
        // "If one of the values is not populated, should be treated as unlimited"
        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(g => g.Price >= parameters.MinPrice.Value);
        }

        if (parameters.MaxPrice.HasValue)
        {
            query = query.Where(g => g.Price <= parameters.MaxPrice.Value);
        }

        return query;
    }
}