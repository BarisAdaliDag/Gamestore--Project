using Gamestore.API.Models.Responses.Games;
using Gamestore.BLL.Services.Abstract;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.API.Controllers;

[ApiController]
[Route("northwind/products")]
public class NorthwindProductsController(
    INorthwindProductService northwindProductService,
    IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GameResponse>>> GetAll()
    {
        var games = await northwindProductService.GetAllAsync();
        return Ok(mapper.Map<IEnumerable<GameResponse>>(games));
    }
}
