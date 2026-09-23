namespace Gamestore.BLL.Services.Abstract;

public interface IExternalAuthenticationService
{
    Task<bool> AuthenticateAsync(string email, string password);
}
