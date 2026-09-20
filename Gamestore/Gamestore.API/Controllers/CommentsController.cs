using Gamestore.API.Constants;
using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Comments;
using Gamestore.BLL.DTOs.Comments;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class CommentsController(ICommentService commentService, IMapper mapper) : ControllerBase
{
    /// <summary>Gets the list of supported ban durations.</summary>
    [HttpGet("ban/durations")]
    [Authorize(Policy = AuthorizationPolicyNames.ModeratorOrHigher)]
    public ActionResult<IReadOnlyList<string>> GetBanDurations()
    {
        return Ok(commentService.GetBanDurations());
    }

    /// <summary>Bans a user from commenting for the given duration.</summary>
    [HttpPost("ban")]
    [Authorize(Policy = AuthorizationPolicyNames.ModeratorOrHigher)]
    [ServiceFilter(typeof(ValidationFilter<BanRequest>))]
    public async Task<IActionResult> Ban([FromBody] BanRequest request)
    {
        var dto = mapper.Map<BanUserDto>(request);
        await commentService.BanAsync(dto);
        return Ok();
    }
}
