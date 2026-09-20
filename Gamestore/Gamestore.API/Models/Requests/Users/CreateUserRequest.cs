namespace Gamestore.API.Models.Requests.Users;

public class CreateUserRequest
{
    public required UserCreateInfoRequest User { get; set; }

    public required ICollection<Guid> Roles { get; set; }

    public required string Password { get; set; }
}
