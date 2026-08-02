namespace Gamestore.BLL.DTOs.Common;

public class PaginationParamsDto
{
    public int Page { get; init; } = 1;

    public int Size { get; init; } = 10;
}
