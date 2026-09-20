using Gamestore.API.Constants;
using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Publishers;
using Gamestore.API.Models.Responses.Games;
using Gamestore.API.Models.Responses.Publishers;
using Gamestore.BLL.DTOs.Publishers;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class PublishersController(
    IPublisherService publisherService,
    IGameService gameService,
    IUnifiedReferenceService unifiedReferenceService,
    IMapper mapper) : ControllerBase
{
    private readonly IPublisherService _publisherService = publisherService;
    private readonly IGameService _gameService = gameService;
    private readonly IUnifiedReferenceService _unifiedReferenceService = unifiedReferenceService;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PublisherResponse>>> GetAll()
    {
        var dtos = await _unifiedReferenceService.GetPublishersAsync();
        var response = _mapper.Map<IEnumerable<PublisherResponse>>(dtos);

        return Ok(response);
    }

    [HttpGet("{companyName}")]
    public async Task<ActionResult<PublisherResponse>> GetByCompanyName(string companyName)
    {
        var dto = await _publisherService.GetByCompanyNameAsync(companyName);
        var response = _mapper.Map<PublisherResponse>(dto);

        return Ok(response);
    }

    [HttpGet("{companyName}/games")]
    public async Task<ActionResult<IEnumerable<GameResponse>>> GetGamesByPublisher(string companyName)
    {
        var dtos = await _gameService.GetByPublisherNameAsync(companyName);
        var response = _mapper.Map<IEnumerable<GameResponse>>(dtos);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOrHigher)]
    [ServiceFilter(typeof(ValidationFilter<CreatePublisherRequest>))]
    public async Task<ActionResult<PublisherResponse>> Create([FromBody] CreatePublisherRequest request)
    {
        var dto = _mapper.Map<CreatePublisherDto>(request);
        var created = await _publisherService.CreateAsync(dto);
        var response = _mapper.Map<PublisherResponse>(created);

        return CreatedAtAction(nameof(GetByCompanyName), new { companyName = response.CompanyName }, response);
    }

    [HttpPut]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOrHigher)]
    [ServiceFilter(typeof(ValidationFilter<UpdatePublisherRequest>))]
    public async Task<ActionResult<PublisherResponse>> Update([FromBody] UpdatePublisherRequest request)
    {
        var dto = _mapper.Map<UpdatePublisherDto>(request);
        var updated = await _publisherService.UpdateAsync(dto);
        var response = _mapper.Map<PublisherResponse>(updated);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOrHigher)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _publisherService.DeleteAsync(id);

        return NoContent();
    }
}
