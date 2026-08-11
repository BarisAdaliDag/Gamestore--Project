using Gamestore.Domain.Entities;

namespace Gamestore.BLL.Helpers;

public static class SortingOptionsHelper
{
    // Sira ONEMLI: US2 bu sirayla dondurur.
    private static readonly (string Name, Func<IQueryable<Game>, IQueryable<Game>> Apply)[] Sorters =
    [
        ("Most popular",   q => q.OrderByDescending(g => g.ViewCount).ThenBy(g => g.Id)),
        ("Most commented", q => q.OrderByDescending(g => g.Comments.Count(c => !c.IsDeleted)).ThenBy(g => g.Id)),
        ("Price ASC",      q => q.OrderBy(g => g.Price).ThenBy(g => g.Id)),
        ("Price DESC",     q => q.OrderByDescending(g => g.Price).ThenBy(g => g.Id)),
        ("New",            q => q.OrderByDescending(g => g.CreatedAt).ThenBy(g => g.Id)),
    ];

    private static readonly string[] Options = [.. Sorters.Select(s => s.Name)];

    public static IReadOnlyList<string> GetOptions() => Options;

    public static IQueryable<Game> ApplySorting(IQueryable<Game> query, string? sort)
    {
        foreach (var (name, apply) in Sorters)
        {
            if (string.Equals(name, sort, StringComparison.OrdinalIgnoreCase))
            {
                return apply(query);
            }
        }

        // Varsayilan: sayfalama icin siralama ZORUNLU
        return query.OrderBy(g => g.Name).ThenBy(g => g.Id);
    }
}
