using Gamestore.API.Constants;
using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Users;
using Gamestore.API.Models.Responses.Roles;
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
    IUserService userService,
    IMapper mapper) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.AdministratorOnly)]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll()
    {
        var users = await userService.GetAllAsync();
        return Ok(mapper.Map<IEnumerable<UserResponse>>(users));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.AdministratorOnly)]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await userService.GetByIdAsync(id);
        return Ok(mapper.Map<UserResponse>(user));
    }

    [HttpGet("{id:guid}/roles")]
    [Authorize(Policy = AuthorizationPolicyNames.AdministratorOnly)]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> GetRoles(Guid id)
    {
        var roles = await userService.GetRolesAsync(id);
        return Ok(mapper.Map<IEnumerable<RoleResponse>>(roles));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.AdministratorOnly)]
    [ServiceFilter(typeof(ValidationFilter<CreateUserRequest>))]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
    {
        var dto = mapper.Map<CreateUserDto>(request);
        var user = await userService.CreateAsync(dto);
        var response = mapper.Map<UserResponse>(user);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

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
