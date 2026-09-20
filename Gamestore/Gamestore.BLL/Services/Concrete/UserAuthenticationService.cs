using Gamestore.BLL.DTOs.Users;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;

namespace Gamestore.BLL.Services.Concrete;

public class UserAuthenticationService(
    IUnitOfWork unitOfWork,
    IPasswordHashService passwordHashService,
    IJwtTokenService jwtTokenService) : IUserAuthenticationService
{
    public async Task<string> LoginAsync(LoginDto dto)
    {
        if (!dto.InternalAuth)
        {
            throw new ValidationException("External authentication is not supported.");
        }

        var user = await unitOfWork.Users.GetByNameWithRolesAsync(dto.Login);

        return user is not null && passwordHashService.VerifyPassword(user, dto.Password)
            ? jwtTokenService.GenerateToken(user)
            : throw new InvalidCredentialsException();
    }
}
