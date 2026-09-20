namespace Gamestore.API.Models.Requests.Users;

public class AccessRequest
{
    public required string TargetPage { get; set; }

    public string? TargetId { get; set; }
}
