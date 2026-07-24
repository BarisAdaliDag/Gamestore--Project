using Gamestore.BLL.Services.Abstract;
using Gamestore.BLL.Services.Concrete;
using Microsoft.Extensions.DependencyInjection;

namespace Gamestore.BLL.DependencyResolvers;

public static class ServiceResolver
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IGenreService, GenreService>();
        services.AddScoped<IPlatformService, PlatformService>();
        services.AddScoped<IPublisherService, PublisherService>();
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
