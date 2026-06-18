using Gamestore.BLL.Services.Abstract;
using Microsoft.Extensions.Caching.Memory;

namespace Gamestore.API.Middlewares;

public class TotalGamesCountMiddleware(RequestDelegate next)
{
    private const string HeaderName = "x-total-numbers-of-games";
    private const string CacheKey = "total-games-count";

    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IGameService gameService, IMemoryCache cache)
    {
        context.Response.OnStarting(async () =>
        {
            if (!cache.TryGetValue(CacheKey, out int count))
            {
                count = await gameService.GetTotalCountAsync();
                cache.Set(CacheKey, count, TimeSpan.FromMinutes(1));
            }

            context.Response.Headers[HeaderName] = count.ToString();
        });

        await _next(context);
    }
}
