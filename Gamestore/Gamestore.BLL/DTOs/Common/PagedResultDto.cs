namespace Gamestore.BLL.DTOs.Common;

public class PagedResultDto<TDto>
{
    public IEnumerable<TDto> Items { get; init; } = [];

    public int CurrentPage { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }
}
