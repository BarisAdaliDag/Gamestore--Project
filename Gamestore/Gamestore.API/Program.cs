using System.Text.Json.Serialization;
using Gamestore.API.DependencyResolvers;
using Gamestore.API.MapperResolvers;
using Gamestore.API.Middlewares;
using Gamestore.BLL.DependencyResolvers;
using Gamestore.BLL.Services.Abstract;
using Gamestore.DAL;
using Serilog;

namespace Gamestore.API;

public static class Program
{
    private const string FileLogOutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] [RequestId: {RequestId}] {Message:lj}{NewLine}{Exception}";

    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, loggerConfiguration) =>
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Logger(exceptionLogger => exceptionLogger
                        .Filter.ByIncludingOnly(logEvent => logEvent.Exception is not null)
                        .WriteTo.File(
                            path: "Logs/exceptions-.txt",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            shared: true,
                            outputTemplate: FileLogOutputTemplate)));

            // Accept string enum values (e.g. "Reply"); raise MaxDepth for deep comment trees
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.MaxDepth = 256;
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddJwtAuthentication(builder.Configuration);

            builder.Services.AddHttpClient("PaymentService", client =>
            {
                var baseUrl = builder.Configuration["PaymentService:BaseUrl"]
                    ?? throw new InvalidOperationException(
                        "PaymentService:BaseUrl configuration is required.");

                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            });

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

            await EnsureDefaultAdministratorAsync(app);

            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthentication();
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

    private static async Task EnsureDefaultAdministratorAsync(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        const string sectionName = "Authentication:DefaultAdministrator";
        var name = app.Configuration[$"{sectionName}:Name"];
        var password = app.Configuration[$"{sectionName}:Password"];

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
        {
            app.Logger.LogWarning(
                "The default Administrator was not created because its development credentials are not configured.");
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IDefaultAdministratorService>();
        var changed = await service.EnsureCreatedAsync(name, password);

        if (changed)
        {
            app.Logger.LogInformation("The default Administrator account was initialized.");
        }
    }
}
