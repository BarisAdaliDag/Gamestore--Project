using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Comments;
using Gamestore.API.Models.Requests.Games;
using Gamestore.API.Models.Responses.Comments;
using Gamestore.API.Models.Responses.Games;
using Gamestore.API.Models.Responses.Genres;
using Gamestore.API.Models.Responses.Platforms;
using Gamestore.API.Models.Responses.Publishers;
using Gamestore.BLL.DTOs.Comments;
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
    IOrderService orderService,
    ICommentService commentService,
    IUnifiedGameCatalogService unifiedGameCatalogService,
    IMapper mapper) : ControllerBase
{
    private readonly IGameService _gameService = gameService;
    private readonly IGenreService _genreService = genreService;
    private readonly IPlatformService _platformService = platformService;
    private readonly IPublisherService _publisherService = publisherService;
    private readonly IOrderService _orderService = orderService;
    private readonly ICommentService _commentService = commentService;
    private readonly IUnifiedGameCatalogService _unifiedGameCatalogService = unifiedGameCatalogService;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    public async Task<ActionResult<PagedGamesResponse>> GetGames([FromQuery] GameQueryRequest request)
    {
        var parameters = _mapper.Map<GameQueryParams>(request);
        var result = await _unifiedGameCatalogService.GetGamesAsync(parameters);

        return Ok(_mapper.Map<PagedGamesResponse>(result));
    }

    [HttpGet("pagination-options")]
    public ActionResult<IReadOnlyList<string>> GetPaginationOptions()
    {
        return Ok(_gameService.GetPaginationOptions());
    }

    [HttpGet("sorting-options")]
    public ActionResult<IReadOnlyList<string>> GetSortingOptions()
    {
        return Ok(_gameService.GetSortingOptions());
    }

    [HttpGet("publish-date-options")]
    public ActionResult<IReadOnlyList<string>> GetPublishDateOptions()
    {
        return Ok(_gameService.GetPublishDateOptions());
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<GameResponse>> GetByKey(string key)
    {
        var dto = await _gameService.GetByKeyAsync(key);
        var response = _mapper.Map<GameResponse>(dto);

        return Ok(response);
    }

    [HttpGet("{key}/publisher")]
    public async Task<ActionResult<PublisherResponse>> GetPublisherByGameKey(string key)
    {
        var dto = await _publisherService.GetByGameKeyAsync(key);
        var response = _mapper.Map<PublisherResponse>(dto);

        return Ok(response);
    }

    [HttpGet("find/{id:guid}")]
    public async Task<ActionResult<GameResponse>> GetById(Guid id)
    {
        var dto = await _gameService.GetByIdAsync(id);
        var response = _mapper.Map<GameResponse>(dto);

        return Ok(response);
    }

    [HttpGet("{key}/genres")]
    public async Task<ActionResult<IEnumerable<GenreResponse>>> GetGenresByGameKey(string key)
    {
        var dtos = await _genreService.GetByGameKeyAsync(key);
        var response = _mapper.Map<IEnumerable<GenreResponse>>(dtos);

        return Ok(response);
    }

    [HttpGet("{key}/platforms")]
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

    [HttpPost("{key}/buy")]
    public async Task<IActionResult> AddToCart(string key)
    {
        await _orderService.AddToCartAsync(key);
        return Ok();
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

    /// <summary>Adds a comment (or reply/quote) to a game.</summary>
    [HttpPost("{key}/comments")]
    [ServiceFilter(typeof(ValidationFilter<CreateCommentRequest>))]
    public async Task<IActionResult> AddComment(string key, [FromBody] CreateCommentRequest request)
    {
        var dto = _mapper.Map<CreateCommentDto>(request);
        await _commentService.AddAsync(key, dto);
        return Ok();
    }

    /// <summary>Gets all comments of a game as a tree.</summary>
    [HttpGet("{key}/comments")]
    public async Task<ActionResult<IEnumerable<CommentResponse>>> GetComments(string key)
    {
        var comments = await _commentService.GetByGameKeyAsync(key);
        return Ok(_mapper.Map<IEnumerable<CommentResponse>>(comments));
    }

    /// <summary>Soft-deletes a comment. Child comments are preserved.</summary>
    [HttpDelete("{key}/comments/{id:guid}")]
    public async Task<IActionResult> DeleteComment(string key, Guid id)
    {
        await _commentService.DeleteAsync(key, id);
        return NoContent();
    }
}
