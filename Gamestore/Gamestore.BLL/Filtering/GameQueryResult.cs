using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Filtering;

public class GameQueryResult
{
    public List<Game> Games { get; init; } = [];

    public int TotalPages { get; init; }

    public int CurrentPage { get; init; }
}