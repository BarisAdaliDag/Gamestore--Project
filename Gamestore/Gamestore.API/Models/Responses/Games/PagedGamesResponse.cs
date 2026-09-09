namespace Gamestore.API.Models.Responses.Games;

public class PagedGamesResponse
{
    public List<GameResponse> Games { get; set; } = [];

    public int TotalPages { get; set; }

    public int CurrentPage { get; set; }
}