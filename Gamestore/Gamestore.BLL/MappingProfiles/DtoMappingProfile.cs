using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.DTOs.Platforms;
using Gamestore.BLL.DTOs.Publishers;
using Gamestore.Domain.Entities;
using Mapster;

namespace Gamestore.BLL.MappingProfiles;

public class DtoMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Game, GameDto>().TwoWays();
        config.NewConfig<Genre, GenreDto>().TwoWays();
        config.NewConfig<Platform, PlatformDto>().TwoWays();
        config.NewConfig<Publisher, PublisherDto>().TwoWays();
    }
}
