namespace Gamestore.BLL.Helpers;

public static class PaginationOptionsHelper
{
    public const string All = "all";

    private static readonly string[] Options = ["10", "20", "50", "100", All];

    public static IReadOnlyList<string> GetOptions() => Options;

    /// <summary>Resolves the page size. null => no pagination ("all").</summary>
    public static int? ResolvePageSize(string? pageCount)
    {
        return string.Equals(pageCount, All, StringComparison.OrdinalIgnoreCase)
            ? null
            : Resolve();

        int Resolve() => int.TryParse(pageCount, out var size) && size > 0 ? size : 10;
    }
}
