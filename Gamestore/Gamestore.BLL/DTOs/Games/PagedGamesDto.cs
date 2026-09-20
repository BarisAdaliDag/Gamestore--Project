namespace Gamestore.BLL.DTOs.Games;

public class PagedGamesDto
{
    public List<GameDto> Games { get; set; } = [];

    public int TotalPages { get; set; }

    public int CurrentPage { get; set; }
}