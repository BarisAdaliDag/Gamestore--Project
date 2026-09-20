using Gamestore.BLL.DTOs.Abstract;

namespace Gamestore.BLL.DTOs.Platforms;

public class PlatformDto : IDto
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;
}
