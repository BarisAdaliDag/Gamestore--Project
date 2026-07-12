using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Games;
using Gamestore.API.Models.Responses.Common;
using Gamestore.API.Models.Responses.Games;
using Gamestore.API.Models.Responses.Genres;
using Gamestore.API.Models.Responses.Platforms;
using Gamestore.API.Models.Responses.Publishers;
using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class GamesController(
    IGameService gameService,
    IGenreService genreService,
    IPlatformService platformService,
    IPublisherService publisherService,
    IMapper mapper) : ControllerBase
{
    private readonly IGameService _gameService = gameService;
    private readonly IGenreService _genreService = genreService;
    private readonly IPlatformService _platformService = platformService;
    private readonly IPublisherService _publisherService = publisherService;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<GameResponse>>> GetAll(
        [FromQuery] GameQueryParameters queryParameters)
    {
        var filter = _mapper.Map<GameFilterDto>(queryParameters);
        var pagedResult = await _gameService.GetFilteredAsync(filter);
        var response = _mapper.Map<PagedResponse<GameResponse>>(pagedResult);

        return Ok(response);
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<GameResponse>> GetByKey(string key)
    {
        var dto = await _gameService.GetByKeyAsync(key);
        var response = _mapper.Map<GameResponse>(dto);

        return Ok(response);
    }

    [HttpGet("{key}/publisher")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<PublisherResponse>> GetPublisherByGameKey(string key)
    {
        var dto = await _publisherService.GetByGameKeyAsync(key);
        var response = _mapper.Map<PublisherResponse>(dto);

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

    [HttpGet("{key}/genres")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IEnumerable<GenreResponse>>> GetGenresByGameKey(string key)
    {
        var dtos = await _genreService.GetByGameKeyAsync(key);
        var response = _mapper.Map<IEnumerable<GenreResponse>>(dtos);

        return Ok(response);
    }

    [HttpGet("{key}/platforms")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IEnumerable<PlatformResponse>>> GetPlatformsByGameKey(string key)
    {
        var dtos = await _platformService.GetByGameKeyAsync(key);
        var response = _mapper.Map<IEnumerable<PlatformResponse>>(dtos);

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
