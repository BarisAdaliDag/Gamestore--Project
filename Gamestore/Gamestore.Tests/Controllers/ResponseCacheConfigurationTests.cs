using System.Reflection;
using Gamestore.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Gamestore.Tests.Controllers;

public class ResponseCacheConfigurationTests
{
    [Fact]
    public void ControllerEndpointsShouldNotUseResponseCacheAttribute()
    {
        var controllerTypes = typeof(GamesController).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .ToList();

        var cachedEndpoints = controllerTypes
            .SelectMany(controllerType => controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttribute<ResponseCacheAttribute>() != null)
                .Select(method => $"{controllerType.Name}.{method.Name}"))
            .ToList();

        var cachedControllers = controllerTypes
            .Where(controllerType => controllerType.GetCustomAttribute<ResponseCacheAttribute>() != null)
            .Select(controllerType => controllerType.Name)
            .ToList();

        Assert.Empty(cachedControllers);
        Assert.Empty(cachedEndpoints);
    }
}
