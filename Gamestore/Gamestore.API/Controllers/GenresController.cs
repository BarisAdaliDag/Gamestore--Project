using Gamestore.API.Filters;
using Gamestore.API.Models.Requests;
using Gamestore.API.Models.Requests.Genres;
using Gamestore.API.Models.Responses.Common;
using Gamestore.API.Models.Responses.Games;
using Gamestore.API.Models.Responses.Genres;
using Gamestore.BLL.DTOs.Common;
using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class GenresController(
    IGenreService genreService,
    IGameService gameService,
    IMapper mapper) : ControllerBase
{
    private readonly IGenreService _genreService = genreService;
    private readonly IGameService _gameService = gameService;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<GenreResponse>>> GetAll(
        [FromQuery] QueryParameters queryParameters)
    {
        if (!Request.Query.ContainsKey("Page") && !Request.Query.ContainsKey("Size"))
        {
            var legacyDtos = await _genreService.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<GenreResponse>>(legacyDtos));
        }

        var paginationParams = _mapper.Map<PaginationParamsDto>(queryParameters);
        var pagedResult = await _genreService.GetPagedAsync(paginationParams);
        var response = _mapper.Map<PagedResponse<GenreResponse>>(pagedResult);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GenreResponse>> GetById(Guid id)
    {
        var dto = await _genreService.GetByIdAsync(id);
        var response = _mapper.Map<GenreResponse>(dto);

        return Ok(response);
    }

    [HttpGet("{id:guid}/genres")]
    public async Task<ActionResult<IEnumerable<GenreResponse>>> GetSubGenres(Guid id)
    {
        var dtos = await _genreService.GetByParentIdAsync(id);
        var response = _mapper.Map<IEnumerable<GenreResponse>>(dtos);

        return Ok(response);
    }

    [HttpGet("{id:guid}/games")]
    public async Task<ActionResult<PagedResponse<GameResponse>>> GetGamesByGenre(
        Guid id,
        [FromQuery] QueryParameters queryParameters)
    {
        if (!Request.Query.ContainsKey("Page") && !Request.Query.ContainsKey("Size"))
        {
            var legacyDtos = await _gameService.GetByGenreIdAsync(id);
            return Ok(_mapper.Map<IEnumerable<GameResponse>>(legacyDtos));
        }

        var paginationParams = _mapper.Map<PaginationParamsDto>(queryParameters);
        var pagedResult = await _gameService.GetByGenreIdAsync(id, paginationParams);
        var response = _mapper.Map<PagedResponse<GameResponse>>(pagedResult);

        return Ok(response);
    }

    [HttpPost]
    [ServiceFilter(typeof(ValidationFilter<CreateGenreRequest>))]
    public async Task<ActionResult<GenreResponse>> Create([FromBody] CreateGenreRequest request)
    {
        var dto = _mapper.Map<CreateGenreDto>(request);
        var created = await _genreService.CreateAsync(dto);
        var response = _mapper.Map<GenreResponse>(created);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut]
    [ServiceFilter(typeof(ValidationFilter<UpdateGenreRequest>))]
    public async Task<ActionResult<GenreResponse>> Update([FromBody] UpdateGenreRequest request)
    {
        var dto = _mapper.Map<UpdateGenreDto>(request);
        var updated = await _genreService.UpdateAsync(dto);
        var response = _mapper.Map<GenreResponse>(updated);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _genreService.DeleteAsync(id);

        return NoContent();
    }
}
