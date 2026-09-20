using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Users;
using Gamestore.API.Models.Responses.Users;
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
}
