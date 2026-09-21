using Gamestore.API.Constants;
using Gamestore.API.Filters;
using Gamestore.API.Models.Requests.Roles;
using Gamestore.API.Models.Responses.Roles;
using Gamestore.BLL.DTOs.Roles;
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

    [HttpPost]
    [ServiceFilter(typeof(ValidationFilter<CreateRoleRequest>))]
    public async Task<ActionResult<RoleResponse>> Create([FromBody] CreateRoleRequest request)
    {
        var dto = mapper.Map<CreateRoleDto>(request);
        var role = await roleService.CreateAsync(dto);
        var response = mapper.Map<RoleResponse>(role);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut]
    [ServiceFilter(typeof(ValidationFilter<UpdateRoleRequest>))]
    public async Task<ActionResult<RoleResponse>> Update([FromBody] UpdateRoleRequest request)
    {
        var dto = mapper.Map<UpdateRoleDto>(request);
        var role = await roleService.UpdateAsync(dto);
        var response = mapper.Map<RoleResponse>(role);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await roleService.DeleteAsync(id);

        return NoContent();
    }
}
