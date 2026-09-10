using Gamestore.BLL.DTOs.Games;

namespace Gamestore.BLL.Services.Abstract;

public interface INorthwindProductService
{
    IReadOnlyList<GameDto> GetAll();
}
