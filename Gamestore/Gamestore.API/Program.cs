using Gamestore.API.DependencyResolvers;
using Gamestore.API.MapperResolvers;
using Gamestore.API.Middlewares;
using Gamestore.BLL.DependencyResolvers;
using Gamestore.DAL;
using Serilog;

namespace Gamestore.API;

public static class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                path: "Logs/app-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}")
            .WriteTo.File(
                path: "Logs/errors-.txt",
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddResponseCaching();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            builder.Services.AddDataAccessLayer(builder.Configuration);
            builder.Services.AddServiceLayer();
            builder.Services.AddDtoMapperService();
            builder.Services.AddVmMapperService();
            builder.Services.AddValidationService();

            var app = builder.Build();

            app.UseMiddleware<GlobalExceptionMiddleware>();

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
        catch (Exception exception)
        {
            Log.Fatal(exception, "Application terminated unexpectedly.");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
