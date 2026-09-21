namespace Gamestore.API.Models.Requests.Users;

public class UserUpdateInfoRequest
{
    public Guid Id { get; set; }

    public required string Name { get; set; }
}
