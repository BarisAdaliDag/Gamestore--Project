using Gamestore.BLL.DTOs.Games;
using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Filtering.Abstract;

/// <summary>
/// A single filtering step in the pipeline.
/// Each filter checks its OWN parameter; if the value is absent,
/// it returns the query unchanged.
/// </summary>
public interface IGameFilter
{
    IQueryable<Game> Apply(IQueryable<Game> query, GameQueryParams parameters);
}