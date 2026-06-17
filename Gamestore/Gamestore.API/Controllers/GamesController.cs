using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Games;
using Gamestore.API.Models.Responses.Games;
using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class GamesController(
    IGameService gameService,
    IMapper mapper) : ControllerBase
{
    private readonly IGameService _gameService = gameService;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IEnumerable<GameResponse>>> GetAll()
    {
        var dtos = await _gameService.GetAllAsync();
        var response = _mapper.Map<IEnumerable<GameResponse>>(dtos);

        return Ok(response);
    }

    [HttpGet("{key}")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<GameResponse>> GetByKey(string key)
    {
        var dto = await _gameService.GetByKeyAsync(key);
        var response = _mapper.Map<GameResponse>(dto);

        return Ok(response);
    }

    [HttpGet("find/{id:guid}")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<GameResponse>> GetById(Guid id)
    {
        var dto = await _gameService.GetByIdAsync(id);
        var response = _mapper.Map<GameResponse>(dto);

        return Ok(response);
    }

    [HttpGet("~/genres/{id:guid}/games")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IEnumerable<GameResponse>>> GetByGenreId(Guid id)
    {
        var dtos = await _gameService.GetByGenreIdAsync(id);
        var response = _mapper.Map<IEnumerable<GameResponse>>(dtos);

        return Ok(response);
    }

    [HttpGet("~/platforms/{id:guid}/games")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IEnumerable<GameResponse>>> GetByPlatformId(Guid id)
    {
        var dtos = await _gameService.GetByPlatformIdAsync(id);
        var response = _mapper.Map<IEnumerable<GameResponse>>(dtos);

        return Ok(response);
    }

    [HttpGet("{key}/file")]
    public async Task<IActionResult> DownloadGameFile(string key)
    {
        var (fileBytes, fileName) = await _gameService.GenerateGameFileAsync(key);

        return File(fileBytes, "text/plain", fileName);
    }

    [HttpPost]
    [ServiceFilter(typeof(ValidationFilter<CreateGameRequest>))]
    public async Task<ActionResult<GameResponse>> Create([FromBody] CreateGameRequest request)
    {
        var dto = _mapper.Map<CreateGameDto>(request);
        var created = await _gameService.CreateAsync(dto);
        var response = _mapper.Map<GameResponse>(created);

        return CreatedAtAction(nameof(GetByKey), new { key = response.Key }, response);
    }

    [HttpPut]
    [ServiceFilter(typeof(ValidationFilter<UpdateGameRequest>))]
    public async Task<ActionResult<GameResponse>> Update([FromBody] UpdateGameRequest request)
    {
        var dto = _mapper.Map<UpdateGameDto>(request);
        var updated = await _gameService.UpdateAsync(dto);
        var response = _mapper.Map<GameResponse>(updated);

        return Ok(response);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        await _gameService.DeleteByKeyAsync(key);

        return NoContent();
    }
}
