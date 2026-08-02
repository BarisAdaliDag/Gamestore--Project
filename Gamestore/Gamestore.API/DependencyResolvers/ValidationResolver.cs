using FluentValidation;
using Gamestore.API.Filters;

namespace Gamestore.API.DependencyResolvers;

public static class ValidationResolver
{
    public static IServiceCollection AddValidationService(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ValidationResolver).Assembly);
        services.AddScoped(typeof(ValidationFilter<>));

        return services;
    }
}
