using Gamestore.API.Constants;
using Gamestore.API.Models.Responses.Roles;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AdministratorOnly)]
[Route("[controller]")]
public class RolesController(IRoleService roleService, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> GetAll()
    {
        var roles = await roleService.GetAllAsync();
        return Ok(mapper.Map<IEnumerable<RoleResponse>>(roles));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleResponse>> GetById(Guid id)
    {
        var role = await roleService.GetByIdAsync(id);
        return Ok(mapper.Map<RoleResponse>(role));
    }
}
