namespace Gamestore.BLL.DTOs.Games;

public class GameFilterDto
{
    public int Page { get; set; } = 1;

    public int Size { get; set; } = 50;

    public string? Name { get; set; }

    public Guid? GenreId { get; set; }

    public Guid? PlatformId { get; set; }
}
