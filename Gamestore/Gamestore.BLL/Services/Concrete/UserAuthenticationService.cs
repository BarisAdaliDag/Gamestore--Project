using Gamestore.BLL.DTOs.Users;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL.UnitOfWork;

namespace Gamestore.BLL.Services.Concrete;

public class UserAuthenticationService(
    IUnitOfWork unitOfWork,
    IPasswordHashService passwordHashService,
    IJwtTokenService jwtTokenService,
    IExternalAuthenticationService externalAuthenticationService) : IUserAuthenticationService
{
    public async Task<string> LoginAsync(LoginDto dto)
    {
        var user = await unitOfWork.Users.GetByNameWithRolesAsync(dto.Login)
            ?? throw new InvalidCredentialsException();

        var credentialsAreValid = dto.InternalAuth
            ? passwordHashService.VerifyPassword(user, dto.Password)
            : await externalAuthenticationService.AuthenticateAsync(dto.Login, dto.Password);

        return credentialsAreValid
            ? jwtTokenService.GenerateToken(user)
            : throw new InvalidCredentialsException();
    }
}
