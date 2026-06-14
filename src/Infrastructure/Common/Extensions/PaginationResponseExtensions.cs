using Retailer.Application.Common.Extensions;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Common.Extensions;

public static class PaginationResponseExtensions
{
    /// <summary>
    /// Applies all filters (search, advanced search, advanced filter, ordering) and pagination to the query.
    /// </summary>
    public static async Task<PaginationResponse<T>> PaginatedListAsync<T>(
       this IQueryable<T> query, PaginationFilter filter, CancellationToken cancellationToken = default)
       where T : class
    {
        // Get count before pagination (but after filters)
        var filteredQuery = query.ApplyFilters(filter);
        int count = await filteredQuery.CountAsync(cancellationToken);

        // Apply pagination
        var paginatedQuery = filteredQuery.PaginateBy(filter);
        var list = await paginatedQuery.ToListAsync(cancellationToken);

        return new PaginationResponse<T>(list, count, filter.PageNumber, filter.PageSize);
    }

    /// <summary>
    /// Applies search and advanced filters without pagination. Use this when you need the filtered query for further operations.
    /// </summary>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, BaseFilter filter)
    {
        // Apply keyword search
        if (!string.IsNullOrEmpty(filter.Keyword))
        {
            query = query.SearchByKeyword(filter.Keyword);
        }

        // Apply advanced search (column-wise search)
        if (filter.AdvancedSearch is not null)
        {
            query = query.AdvancedSearch(filter.AdvancedSearch);
        }

        // Apply advanced filters (complex filtering with operators)
        if (filter.AdvancedFilter is not null)
        {
            query = query.ApplyAdvancedFilter(filter.AdvancedFilter);
        }

        return query;
    }
}

