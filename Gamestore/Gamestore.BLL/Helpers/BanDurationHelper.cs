namespace Gamestore.BLL.Helpers;

public static class BanDurationHelper
{
    public const string Permanent = "permanent";

    // Order is IMPORTANT: US4 must return in this order.
    // Single source of truth: the list, validation, and resolution all read from here.
    private static readonly (string Name, Func<DateTime, DateTime?> Resolve)[] Durations =
    [
        ("1 hour", from => from.AddHours(1)),
        ("1 day", from => from.AddDays(1)),
        ("1 week", from => from.AddDays(7)),
        ("1 month", from => from.AddMonths(1)),
        (Permanent, _ => null),
    ];

    public static IReadOnlyList<string> GetDurations()
    {
        return [.. Durations.Select(d => d.Name)];
    }

    public static bool IsSupported(string? duration)
    {
        return !string.IsNullOrWhiteSpace(duration) && Find(duration) is not null;
    }

    /// <summary>
    /// Converts the given duration to an end date. Returns null if the ban is permanent.
    /// </summary>
    public static DateTime? Resolve(string duration, DateTime from)
    {
        var match = Find(duration)
            ?? throw new ArgumentException($"Unsupported ban duration: '{duration}'.", nameof(duration));

        return match.Resolve(from);
    }

    private static (string Name, Func<DateTime, DateTime?> Resolve)? Find(string duration)
    {
        foreach (var item in Durations)
        {
            if (string.Equals(item.Name, duration, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }
}