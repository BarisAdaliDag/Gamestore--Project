using System.ComponentModel.DataAnnotations;

namespace Gamestore.API.Models.Requests;

public class QueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int Size { get; init; } = 10;
}
