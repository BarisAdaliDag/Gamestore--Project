using Gamestore.API.Models.Requests;
using Gamestore.API.Models.Requests.Games;
using Gamestore.API.Models.Requests.Genres;
using Gamestore.API.Models.Requests.Platforms;
using Gamestore.API.Models.Responses.Common;
using Gamestore.API.Models.Responses.Games;
using Gamestore.API.Models.Responses.Genres;
using Gamestore.API.Models.Responses.Platforms;
using Gamestore.BLL.DTOs.Common;
using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.DTOs.Platforms;
using Mapster;

namespace Gamestore.API.MappingProfiles;

public class VmMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GameDto, GameResponse>();
        config.NewConfig<GenreDto, GenreResponse>();
        config.NewConfig<PlatformDto, PlatformResponse>();
        config.NewConfig<QueryParameters, PaginationParamsDto>();
        config.NewConfig<GameQueryParameters, GameFilterDto>();
        config.NewConfig<PagedResultDto<GameDto>, PagedResponse<GameResponse>>();
        config.NewConfig<PagedResultDto<GenreDto>, PagedResponse<GenreResponse>>();
        config.NewConfig<PagedResultDto<PlatformDto>, PagedResponse<PlatformResponse>>();

        config.NewConfig<CreateGameRequest, CreateGameDto>();
        config.NewConfig<GameCreateInfoRequest, GameCreateInfoDto>();
        config.NewConfig<UpdateGameRequest, UpdateGameDto>();
        config.NewConfig<GameUpdateInfoRequest, GameUpdateInfoDto>();

        config.NewConfig<CreateGenreRequest, CreateGenreDto>();
        config.NewConfig<GenreCreateInfoRequest, GenreCreateInfoDto>();
        config.NewConfig<UpdateGenreRequest, UpdateGenreDto>();
        config.NewConfig<GenreUpdateInfoRequest, GenreUpdateInfoDto>();

        config.NewConfig<CreatePlatformRequest, CreatePlatformDto>();
        config.NewConfig<PlatformCreateInfoRequest, PlatformCreateInfoDto>();
        config.NewConfig<UpdatePlatformRequest, UpdatePlatformDto>();
        config.NewConfig<PlatformUpdateInfoRequest, PlatformUpdateInfoDto>();
    }
}
