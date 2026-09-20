namespace Gamestore.API.Models.Requests.Users;

public class LoginModelRequest
{
    public required string Login { get; set; }

    public required string Password { get; set; }

    public bool InternalAuth { get; set; }
}
