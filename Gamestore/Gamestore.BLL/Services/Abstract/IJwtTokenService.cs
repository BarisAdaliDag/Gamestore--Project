using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Services.Abstract;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
