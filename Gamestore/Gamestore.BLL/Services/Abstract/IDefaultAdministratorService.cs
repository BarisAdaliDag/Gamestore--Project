namespace Gamestore.BLL.Services.Abstract;

public interface IDefaultAdministratorService
{
    Task<bool> EnsureCreatedAsync(string name, string password);
}
