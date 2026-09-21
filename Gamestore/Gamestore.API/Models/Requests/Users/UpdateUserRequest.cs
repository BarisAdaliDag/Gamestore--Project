namespace Gamestore.API.Models.Requests.Users;

public class UpdateUserRequest
{
    public required UserUpdateInfoRequest User { get; set; }

    public required ICollection<Guid> Roles { get; set; }

    public required string Password { get; set; }
}
