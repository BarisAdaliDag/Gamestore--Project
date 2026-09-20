using Gamestore.BLL.DTOs.Users;

namespace Gamestore.BLL.Services.Abstract;

public interface IUserAuthenticationService
{
    Task<string> LoginAsync(LoginDto dto);
}
