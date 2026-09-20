using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Filtering.Filters;
using Gamestore.Domain.Entities;

namespace Gamestore.Tests.Filtering;

public class NameFilterTests
{
    [Fact]
    public void ApplyShouldNotFilterWhenNameIsShorterThanThreeChars()
    {
        var result = new NameFilter().Apply(Games(), new GameQueryParams { Name = "Wi" });

        Assert.Equal(2, result.Count());   // filter did not run
    }

    [Fact]
    public void ApplyShouldFilterWhenNameHasThreeOrMoreChars()
    {
        var result = new NameFilter().Apply(Games(), new GameQueryParams { Name = "Wit" });

        Assert.Single(result);
    }

    [Fact]
    public void ApplyShouldNotFilterWhenNameIsNull()
    {
        var result = new NameFilter().Apply(Games(), new GameQueryParams());

        Assert.Equal(2, result.Count());
    }

    private static IQueryable<Game> Games() => new List<Game>
    {
        new() { Name = "Witcher", Key = "witcher" },
        new() { Name = "Doom", Key = "doom" },
    }.AsQueryable();
}