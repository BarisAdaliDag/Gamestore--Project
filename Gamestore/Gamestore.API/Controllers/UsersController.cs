using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Users;
using Gamestore.API.Models.Responses.Users;
using Gamestore.BLL.Constants;
using Gamestore.BLL.DTOs.Users;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController(
    IUserAuthenticationService authenticationService,
    IPageAccessService pageAccessService,
    IMapper mapper) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ServiceFilter(typeof(ValidationFilter<LoginRequest>))]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var dto = mapper.Map<LoginDto>(request.Model);
        var token = await authenticationService.LoginAsync(dto);

        return Ok(new LoginResponse { Token = token });
    }

    [AllowAnonymous]
    [HttpPost("access")]
    [ServiceFilter(typeof(ValidationFilter<AccessRequest>))]
    public ActionResult<bool> CheckAccess([FromBody] AccessRequest request)
    {
        var roles = User.FindAll(JwtClaimNames.Role).Select(claim => claim.Value);
        var hasAccess = pageAccessService.HasAccess(request.TargetPage, roles);

        return Ok(hasAccess);
    }
}
