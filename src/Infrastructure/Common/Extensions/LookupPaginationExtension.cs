using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Retailer.Application.Common.Lookup;
using Retailer.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Retailer.Infrastructure.Common.Extensions;

public static class LookupPaginationExtension
{
    public static async Task<LookupPaginatedResponse<T>> LookupPaginatedListAsync<T>(this IQueryable<T> query, LookupFilterRequest request, bool isSearchRequired = true)
    {
        if (isSearchRequired)
        {
            var searchableProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name != "Id" && p.PropertyType == typeof(string));

            Expression body = null;
            var parameter = Expression.Parameter(typeof(T), "x");

            if (request != null && !string.IsNullOrEmpty(request.Keyword))
            {
                string keyword = request.Keyword.ToLower();

                foreach (var property in searchableProperties)
                {
                    var propertyAccess = Expression.Property(parameter, property);
                    var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                    var toLowerPropertyAccess = Expression.Call(propertyAccess, toLowerMethod);

                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    var constant = Expression.Constant(keyword, typeof(string));
                    var fieldCondition = Expression.Call(toLowerPropertyAccess, containsMethod, constant);

                    body = body == null
                        ? fieldCondition
                        : Expression.OrElse(body, fieldCondition);
                }

                if (body != null)
                {
                    var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                    query = query.Where(lambda);
                }
            }
        }

        int totalCount = await query.CountAsync();

        if (request != null)
        {
            query = PaginateBy(query, request);
        }
        else
        {
            request = new LookupFilterRequest
            {
                PageNumber = 1,
                PageSize = 10
            };
        }

        var list = await query.ToListAsync();

        return new LookupPaginatedResponse<T>(list, totalCount, request.PageNumber, request.PageSize);
    }

    public static IQueryable<T> PaginateBy<T>(this IQueryable<T> query, LookupFilterRequest filter)
    {
        if (filter != null)
        {
            if (filter.PageNumber <= 0)
            {
                filter.PageNumber = 1;
            }

            if (filter.PageSize <= 0)
            {
                filter.PageSize = 10;
            }

            if (filter.PageNumber > 1)
            {
                query = query.Skip((filter.PageNumber - 1) * filter.PageSize);
            }

            query = query.Take(filter.PageSize);
        }

        return query;
    }
}
