using Gamestore.API.Models.Requests.Comments;
using Gamestore.BLL.DTOs.Comments;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class CommentsController(ICommentService commentService, IMapper mapper) : ControllerBase
{
    /// <summary>Gets the list of supported ban durations.</summary>
    [HttpGet("ban/durations")]
    public ActionResult<IReadOnlyList<string>> GetBanDurations()
    {
        return Ok(commentService.GetBanDurations());
    }

    /// <summary>Bans a user from commenting for the given duration.</summary>
    [HttpPost("ban")]
    public async Task<IActionResult> Ban([FromBody] BanRequest request)
    {
        var dto = mapper.Map<BanUserDto>(request);
        await commentService.BanAsync(dto);
        return Ok();
    }
}