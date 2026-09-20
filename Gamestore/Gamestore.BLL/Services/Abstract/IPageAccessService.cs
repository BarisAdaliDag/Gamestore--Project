namespace Gamestore.BLL.Services.Abstract;

public interface IPageAccessService
{
    bool HasAccess(string targetPage, IEnumerable<string> roles);
}
