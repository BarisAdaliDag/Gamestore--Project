namespace Gamestore.API.Models.Responses.Common;

public class PagedResponse<TResponse>
{
    public IEnumerable<TResponse> Items { get; init; } = [];

    public int CurrentPage { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }
}
