using Gamestore.API.Models.Requests.Platforms;
using Gamestore.API.Models.Responses.Games;
using Gamestore.API.Models.Responses.Platforms;
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
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IEnumerable<PlatformResponse>>> GetAll()
    {
        var dtos = await _platformService.GetAllAsync();
        var response = _mapper.Map<IEnumerable<PlatformResponse>>(dtos);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<PlatformResponse>> GetById(Guid id)
    {
        var dto = await _platformService.GetByIdAsync(id);
        var response = _mapper.Map<PlatformResponse>(dto);

        return Ok(response);
    }

    [HttpGet("{id:guid}/games")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IEnumerable<GameResponse>>> GetGamesByPlatform(Guid id)
    {
        var dtos = await _gameService.GetByPlatformIdAsync(id);
        var response = _mapper.Map<IEnumerable<GameResponse>>(dtos);

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<PlatformResponse>> Create([FromBody] CreatePlatformRequest request)
    {
        var dto = _mapper.Map<CreatePlatformDto>(request);
        var created = await _platformService.CreateAsync(dto);
        var response = _mapper.Map<PlatformResponse>(created);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut]
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
