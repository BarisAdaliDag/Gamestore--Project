using System.Reflection;
using Mapster;

namespace Gamestore.API.MapperResolvers;

public static class VmMapperResolver
{
    public static IServiceCollection AddVmMapperService(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        return services;
    }
}
