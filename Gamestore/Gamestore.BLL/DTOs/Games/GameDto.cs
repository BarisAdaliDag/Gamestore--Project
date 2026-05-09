using Gamestore.BLL.DTOs.Abstract;

namespace Gamestore.BLL.DTOs.Games;

public class GameDto : IDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }
}