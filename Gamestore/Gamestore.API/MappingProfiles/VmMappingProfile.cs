using Gamestore.API.Models.Requests;
using Gamestore.API.Models.Requests.Comments;
using Gamestore.API.Models.Requests.Games;
using Gamestore.API.Models.Requests.Genres;
using Gamestore.API.Models.Requests.Orders;
using Gamestore.API.Models.Requests.Platforms;
using Gamestore.API.Models.Requests.Publishers;
using Gamestore.API.Models.Requests.Users;
using Gamestore.API.Models.Responses.Comments;
using Gamestore.API.Models.Responses.Common;
using Gamestore.API.Models.Responses.Games;
using Gamestore.API.Models.Responses.Genres;
using Gamestore.API.Models.Responses.Orders;
using Gamestore.API.Models.Responses.Platforms;
using Gamestore.API.Models.Responses.Publishers;
using Gamestore.API.Models.Responses.Roles;
using Gamestore.API.Models.Responses.Users;
using Gamestore.BLL.DTOs.Comments;
using Gamestore.BLL.DTOs.Common;
using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.DTOs.Orders;
using Gamestore.BLL.DTOs.Platforms;
using Gamestore.BLL.DTOs.Publishers;
using Gamestore.BLL.DTOs.Roles;
using Gamestore.BLL.DTOs.Users;
using Gamestore.Domain.Enums;
using Mapster;

namespace Gamestore.API.MappingProfiles;

public class VmMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GameDto, GameResponse>();
        config.NewConfig<GenreDto, GenreResponse>();
        config.NewConfig<PlatformDto, PlatformResponse>();
        config.NewConfig<PublisherDto, PublisherResponse>();
        config.NewConfig<QueryParameters, PaginationParamsDto>();
        config.NewConfig<GameQueryRequest, GameQueryParams>();
        config.NewConfig<PagedGamesDto, PagedGamesResponse>();
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

        config.NewConfig<CreatePublisherRequest, CreatePublisherDto>();
        config.NewConfig<PublisherCreateInfoRequest, PublisherCreateInfoDto>();
        config.NewConfig<UpdatePublisherRequest, UpdatePublisherDto>();
        config.NewConfig<PublisherUpdateInfoRequest, PublisherUpdateInfoDto>();

        config.NewConfig<OrderDto, OrderResponse>();
        config.NewConfig<OrderGameDto, OrderGameResponse>();
        config.NewConfig<PaymentMethodDto, PaymentMethodResponse>();
        config.NewConfig<PaymentResultDto, PaymentResultResponse>();
        config.NewConfig<PaymentRequest, PaymentRequestDto>();
        config.NewConfig<VisaPaymentModel, VisaPaymentModelDto>();

        // --- Comments ---

        // Nested request -> nested DTO (pure copy)
        config.NewConfig<CommentCreateInfoRequest, CommentCreateInfoDto>();

        // Boundary normalization: the UI sends empty strings for optional fields.
        // Convert empty/null strings to null Guid / None enum before mapping.
        config.NewConfig<CreateCommentRequest, CreateCommentDto>()
            .MapWith(src => new CreateCommentDto
            {
                Comment = src.Comment.Adapt<CommentCreateInfoDto>(),
                ParentId = string.IsNullOrWhiteSpace(src.ParentId) ? null : Guid.Parse(src.ParentId),
                Action = string.IsNullOrWhiteSpace(src.Action) ? CommentAction.None : Enum.Parse<CommentAction>(src.Action),
            });

        config.NewConfig<BanRequest, BanUserDto>();
        config.NewConfig<LoginModelRequest, LoginDto>();
        config.NewConfig<CreateUserRequest, CreateUserDto>();
        config.NewConfig<UserCreateInfoRequest, UserCreateInfoDto>();
        config.NewConfig<UpdateUserRequest, UpdateUserDto>();
        config.NewConfig<UserUpdateInfoRequest, UserUpdateInfoDto>();
        config.NewConfig<UserDto, UserResponse>();
        config.NewConfig<RoleDto, RoleResponse>();

        // Recursive but SAFE: the tree is already built, no computation, no back-reference.
        // (Comparison: Comment -> CommentDto mapping is intentionally absent, see DtoMappingProfile)
        config.NewConfig<CommentDto, CommentResponse>();
    }
}
