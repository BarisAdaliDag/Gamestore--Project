using Gamestore.BLL.DTOs.Games;
using Gamestore.BLL.Filtering.Abstract;
using Gamestore.BLL.Helpers;
using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.BLL.Filtering;

/// <summary>
/// NFR2: filter -> sort -> paginate chain.
/// All steps operate on IQueryable => a SINGLE SQL statement is produced.
/// </summary>
public class GameQueryPipeline(IEnumerable<IGameFilter> filters)
{
    public async Task<GameQueryResult> ExecuteAsync(IQueryable<Game> source, GameQueryParams parameters)
    {
        // ================= 1) FILTER =================
        // Each filter is applied in order. Whether a filter applies is
        // decided by the filter ITSELF -> no if here.
        var query = filters.Aggregate(source, (current, filter) => filter.Apply(current, parameters));

        // ================= 2) COUNT =================
        // After filters, before pagination
        var totalCount = await query.CountAsync();

        // ================= 3) SORT =================
        query = SortingOptionsHelper.ApplySorting(query, parameters.Sort);

        // ================= 4) PAGINATE =================
        var pageSize = PaginationOptionsHelper.ResolvePageSize(parameters.PageCount);

        var totalPages = pageSize is null
            ? 1
            : Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize.Value));

        var currentPage = Math.Clamp(parameters.Page, 1, totalPages);

        if (pageSize is not null)
        {
            query = query
                .Skip(pageSize.Value * (currentPage - 1))
                .Take(pageSize.Value);
        }

        // ================= 5) EXECUTE =================
        var games = await query.ToListAsync();

        return new GameQueryResult
        {
            Games = games,
            TotalPages = totalPages,
            CurrentPage = currentPage,
        };
    }
}