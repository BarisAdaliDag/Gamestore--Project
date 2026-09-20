using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Services.Abstract;

public interface IGameMaterializationService
{
    Task<Game?> GetOrCreateAsync(string gameKey);
}
