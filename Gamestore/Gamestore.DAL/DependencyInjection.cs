using Gamestore.DAL.Context;
using Gamestore.DAL.Mongo;
using Gamestore.DAL.Mongo.Repositories.Abstract;
using Gamestore.DAL.Mongo.Repositories.Concrete;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.DAL.Repositories.Concrete;
using Gamestore.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gamestore.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccessLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<GamestoreDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton<IMongoDbContext, MongoDbContext>();
        services.AddScoped<INorthwindProductRepository, NorthwindProductRepository>();

        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IGenreRepository, GenreRepository>();
        services.AddScoped<IPlatformRepository, PlatformRepository>();
        services.AddScoped<IPublisherRepository, PublisherRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        return services;
    }
}
