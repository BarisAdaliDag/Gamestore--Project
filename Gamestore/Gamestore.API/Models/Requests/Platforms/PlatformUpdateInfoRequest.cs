namespace Gamestore.API.Models.Requests.Platforms;

public class PlatformUpdateInfoRequest
{
    public Guid Id { get; set; }

    public required string Type { get; set; }
}
