using Gamestore.BLL.Filtering;
using Gamestore.BLL.Filtering.Abstract;
using Gamestore.BLL.Filtering.Filters;
using Gamestore.BLL.Services.Abstract;
using Gamestore.BLL.Services.Concrete;
using Gamestore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Gamestore.BLL.DependencyResolvers;

public static class ServiceResolver
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IUnifiedGameCatalogService, UnifiedGameCatalogService>();
        services.AddScoped<IGameLookupService, GameLookupService>();
        services.AddScoped<IUnifiedReferenceService, UnifiedReferenceService>();
        services.AddScoped<IGameMaterializationService, GameMaterializationService>();
        services.AddScoped<INorthwindProductService, NorthwindProductService>();
        services.AddScoped<IGenreService, GenreService>();
        services.AddScoped<IPlatformService, PlatformService>();
        services.AddScoped<IPublisherService, PublisherService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        // Time abstraction (for PublishDateFilter)
        services.AddSingleton(TimeProvider.System);

        // Pipeline filters - REGISTRATION ORDER = EXECUTION ORDER
        services.AddScoped<IGameFilter, GenreFilter>();
        services.AddScoped<IGameFilter, PlatformFilter>();
        services.AddScoped<IGameFilter, PublisherFilter>();
        services.AddScoped<IGameFilter, PriceRangeFilter>();
        services.AddScoped<IGameFilter, PublishDateFilter>();
        services.AddScoped<IGameFilter, NameFilter>();

        services.AddScoped<GameQueryPipeline>();

        return services;
    }
}
