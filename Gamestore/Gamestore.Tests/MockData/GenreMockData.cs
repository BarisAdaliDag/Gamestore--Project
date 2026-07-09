using Gamestore.Domain.Entities;

namespace Gamestore.Tests.MockData;

public static class GenreMockData
{
    public static List<Genre> GetGenres()
    {
        return
        [
            new() { Id = Guid.NewGuid(), Name = "Action" },
            new() { Id = Guid.NewGuid(), Name = "RPG" },
            new() { Id = Guid.NewGuid(), Name = "Strategy" },
            new() { Id = Guid.NewGuid(), Name = "Sports" },
        ];
    }
}
