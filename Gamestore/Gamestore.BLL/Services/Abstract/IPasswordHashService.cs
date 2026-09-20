using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Services.Abstract;

public interface IPasswordHashService
{
    string HashPassword(User user, string password);

    bool VerifyPassword(User user, string password);
}
