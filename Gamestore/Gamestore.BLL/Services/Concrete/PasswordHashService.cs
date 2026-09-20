using Gamestore.BLL.Services.Abstract;
using Gamestore.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Gamestore.BLL.Services.Concrete;

public class PasswordHashService(IPasswordHasher<User> passwordHasher) : IPasswordHashService
{
    public string HashPassword(User user, string password)
    {
        return passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string password)
    {
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
