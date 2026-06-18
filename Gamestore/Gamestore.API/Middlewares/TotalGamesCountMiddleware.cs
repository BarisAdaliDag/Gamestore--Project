using Gamestore.BLL.Services.Abstract;

namespace Gamestore.API.Middlewares;

public class TotalGamesCountMiddleware(RequestDelegate next)
{
    private const string HeaderName = "x-total-numbers-of-games";

    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IGameService gameService)
    {
        context.Response.OnStarting(async () =>
        {
            var count = await gameService.GetTotalCountAsync();
            context.Response.Headers[HeaderName] = count.ToString();
        });

        await _next(context);
    }
}
