using Gamestore.API.Swagger;
using Microsoft.OpenApi.Models;

namespace Gamestore.API.DependencyResolvers;

public static class SwaggerResolver
{
    public const string BearerScheme = "Bearer";

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(
                BearerScheme,
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Description =
                        "Enter the JWT returned by POST /users/login. Swagger adds the Bearer prefix automatically.",
                });
            options.OperationFilter<AuthorizationOperationFilter>();
        });

        return services;
    }
}
