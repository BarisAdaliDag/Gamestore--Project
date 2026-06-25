namespace Gamestore.BLL.DTOs.Games;

public class CreateGameDto
{
    public required GameCreateInfoDto Game { get; set; }

    public required ICollection<Guid> Genres { get; set; }

    public required ICollection<Guid> Platforms { get; set; }
}
