using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Genres;
using Gamestore.API.Models.Responses.Genres;
using Gamestore.BLL.DTOs.Genres;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class GenresController(
    IGenreService genreService,
    IMapper mapper) : ControllerBase
{
    private readonly IGenreService _genreService = genreService;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IEnumerable<GenreResponse>>> GetAll()
    {
        var dtos = await _genreService.GetAllAsync();
        var response = _mapper.Map<IEnumerable<GenreResponse>>(dtos);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<GenreResponse>> GetById(Guid id)
    {
        var dto = await _genreService.GetByIdAsync(id);
        var response = _mapper.Map<GenreResponse>(dto);

        return Ok(response);
    }

    [HttpGet("{id:guid}/genres")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IEnumerable<GenreResponse>>> GetSubGenres(Guid id)
    {
        var dtos = await _genreService.GetByParentIdAsync(id);
        var response = _mapper.Map<IEnumerable<GenreResponse>>(dtos);

        return Ok(response);
    }

    [HttpGet("~/games/{key}/genres")]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<IEnumerable<GenreResponse>>> GetByGameKey(string key)
    {
        var dtos = await _genreService.GetByGameKeyAsync(key);
        var response = _mapper.Map<IEnumerable<GenreResponse>>(dtos);

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
