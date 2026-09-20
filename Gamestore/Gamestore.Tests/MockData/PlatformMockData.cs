using Gamestore.Domain.Entities;

namespace Gamestore.Tests.MockData;

public static class PlatformMockData
{
    public static List<Platform> GetPlatforms()
    {
        return
        [
            new() { Id = Guid.NewGuid(), Type = "Mobile" },
            new() { Id = Guid.NewGuid(), Type = "Desktop" },
            new() { Id = Guid.NewGuid(), Type = "Console" },
        ];
    }
}
