using Gamestore.API.DependencyResolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gamestore.API.Swagger;

public class AuthorizationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var methodMetadata = context.MethodInfo.GetCustomAttributes(true);
        var controllerMetadata = context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? [];

        if (methodMetadata.OfType<IAllowAnonymous>().Any()
            || controllerMetadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        var requiresAuthorization = methodMetadata.OfType<IAuthorizeData>().Any()
            || controllerMetadata.OfType<IAuthorizeData>().Any();

        if (!requiresAuthorization)
        {
            return;
        }

        var bearerScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = SwaggerResolver.BearerScheme,
            },
        };

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [bearerScheme] = [],
            },
        ];
        operation.Responses.TryAdd(
            StatusCodes.Status401Unauthorized.ToString(),
            new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.TryAdd(
            StatusCodes.Status403Forbidden.ToString(),
            new OpenApiResponse { Description = "Forbidden" });
    }
}
