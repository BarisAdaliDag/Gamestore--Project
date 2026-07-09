namespace Gamestore.BLL.DTOs.Games;

public class GameCreateInfoDto
{
    public required string Name { get; set; }

    public string? Key { get; set; }

    public string? Description { get; set; }
}