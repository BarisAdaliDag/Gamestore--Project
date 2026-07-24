using Gamestore.API.Filters;
using Gamestore.API.Models.Requests;
using Gamestore.API.Models.Requests.Platforms;
using Gamestore.API.Models.Responses.Common;
using Gamestore.API.Models.Responses.Games;
using Gamestore.API.Models.Responses.Platforms;
using Gamestore.BLL.DTOs.Common;
using Gamestore.BLL.DTOs.Platforms;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class PlatformsController(
    IPlatformService platformService,
    IGameService gameService,
    IMapper mapper) : ControllerBase
{
    private readonly IPlatformService _platformService = platformService;
    private readonly IGameService _gameService = gameService;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<PlatformResponse>>> GetAll(
        [FromQuery] QueryParameters queryParameters)
    {
        if (!Request.Query.ContainsKey("Page") && !Request.Query.ContainsKey("Size"))
        {
            var legacyDtos = await _platformService.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<PlatformResponse>>(legacyDtos));
        }

        var paginationParams = _mapper.Map<PaginationParamsDto>(queryParameters);
        var pagedResult = await _platformService.GetPagedAsync(paginationParams);
        var response = _mapper.Map<PagedResponse<PlatformResponse>>(pagedResult);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlatformResponse>> GetById(Guid id)
    {
        var dto = await _platformService.GetByIdAsync(id);
        var response = _mapper.Map<PlatformResponse>(dto);

        return Ok(response);
    }

    [HttpGet("{id:guid}/games")]
    public async Task<ActionResult<PagedResponse<GameResponse>>> GetGamesByPlatform(
        Guid id,
        [FromQuery] QueryParameters queryParameters)
    {
        if (!Request.Query.ContainsKey("Page") && !Request.Query.ContainsKey("Size"))
        {
            var legacyDtos = await _gameService.GetByPlatformIdAsync(id);
            return Ok(_mapper.Map<IEnumerable<GameResponse>>(legacyDtos));
        }

        var paginationParams = _mapper.Map<PaginationParamsDto>(queryParameters);
        var pagedResult = await _gameService.GetByPlatformIdAsync(id, paginationParams);
        var response = _mapper.Map<PagedResponse<GameResponse>>(pagedResult);

        return Ok(response);
    }

    [HttpPost]
    [ServiceFilter(typeof(ValidationFilter<CreatePlatformRequest>))]
    public async Task<ActionResult<PlatformResponse>> Create([FromBody] CreatePlatformRequest request)
    {
        var dto = _mapper.Map<CreatePlatformDto>(request);
        var created = await _platformService.CreateAsync(dto);
        var response = _mapper.Map<PlatformResponse>(created);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut]
    [ServiceFilter(typeof(ValidationFilter<UpdatePlatformRequest>))]
    public async Task<ActionResult<PlatformResponse>> Update([FromBody] UpdatePlatformRequest request)
    {
        var dto = _mapper.Map<UpdatePlatformDto>(request);
        var updated = await _platformService.UpdateAsync(dto);
        var response = _mapper.Map<PlatformResponse>(updated);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _platformService.DeleteAsync(id);

        return NoContent();
    }
}
