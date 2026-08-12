namespace Gamestore.BLL.Helpers;

public static class PublishDateOptionsHelper
{
    // ORDER MATTERS: US3 returns them in this order.
    private static readonly (string Name, Func<DateTime, DateTime> Cutoff)[] Options =
    [
        ("last week",  now => now.AddDays(-7)),
        ("last month", now => now.AddMonths(-1)),
        ("last year",  now => now.AddYears(-1)),
        ("2 years",    now => now.AddYears(-2)),
        ("3 years",    now => now.AddYears(-3)),
    ];

    private static readonly string[] Names = [.. Options.Select(o => o.Name)];

    public static IReadOnlyList<string> GetOptions() => Names;

    /// <summary>Returns the threshold date. Unknown value => null (no filter is applied).</summary>
    public static DateTime? ResolveCutoff(string? option, DateTime from)
    {
        foreach (var (name, cutoff) in Options)
        {
            if (string.Equals(name, option, StringComparison.OrdinalIgnoreCase))
            {
                return cutoff(from);
            }
        }

        return null;
    }
}
