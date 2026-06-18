using Gamestore.API.DependencyResolvers;
using Gamestore.API.MapperResolvers;
using Gamestore.API.Middlewares;
using Gamestore.BLL.DependencyResolvers;
using Gamestore.DAL;

namespace Gamestore.API;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddMemoryCache();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddResponseCaching();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("x-total-numbers-of-games"));
        });

        builder.Services.AddDataAccessLayer(builder.Configuration);
        builder.Services.AddServiceLayer();
        builder.Services.AddDtoMapperService();
        builder.Services.AddVmMapperService();
        builder.Services.AddValidationService();

        var app = builder.Build();

        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseMiddleware<TotalGamesCountMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors();
        app.UseResponseCaching();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
