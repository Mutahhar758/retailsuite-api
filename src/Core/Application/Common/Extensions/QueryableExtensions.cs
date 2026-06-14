using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Retailer.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> SearchBy<T>(this IQueryable<T> query, BaseFilter filter)
    {
        query = query.SearchByKeyword(filter.Keyword);
        
        if (filter.AdvancedSearch is not null)
        {
            query = query.AdvancedSearch(filter.AdvancedSearch);
        }

        if (filter.AdvancedFilter is not null)
        {
            query = query.ApplyAdvancedFilter(filter.AdvancedFilter);
        }

        return query;
    }

    public static IQueryable<T> PaginateBy<T>(this IQueryable<T> query, PaginationFilter filter)
    {
        if (filter.PageNumber <= 0)
        {
            filter.PageNumber = 1;
        }

        if (filter.PageSize <= 0)
        {
            filter.PageSize = 10;
        }

        if (filter.OrderBy?.Any() is true)
        {
            query = query.ApplyOrdering(filter.OrderBy);
        }

        if (filter.PageNumber > 1)
        {
            query = query.Skip((filter.PageNumber - 1) * filter.PageSize);
        }

        return query.Take(filter.PageSize);
    }

    public static IQueryable<T> SearchByKeyword<T>(this IQueryable<T> query, string? keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return query;
        }

        return query.AdvancedSearch(new Search { Keyword = keyword });
    }

    public static IQueryable<T> AdvancedSearch<T>(this IQueryable<T> query, Search? search)
    {
        if (string.IsNullOrEmpty(search?.Keyword))
        {
            return query;
        }

        var keyword = search.Keyword.ToLower();
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        // If specific fields are provided, search only those fields
        if (search.Fields?.Any() is true)
        {
            foreach (var field in search.Fields)
            {
                var propertyExpression = GetPropertyExpression(field, parameter);
                
                if (propertyExpression.Type == typeof(string))
                {
                    var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

                    var propertyToLower = Expression.Call(propertyExpression, toLowerMethod!);
                    var keywordConstant = Expression.Constant(keyword);
                    var containsCall = Expression.Call(propertyToLower, containsMethod!, keywordConstant);

                    var nullCheck = Expression.NotEqual(propertyExpression, Expression.Constant(null, typeof(string)));
                    var safeContains = Expression.AndAlso(nullCheck, containsCall);

                    combinedExpression = combinedExpression == null
                        ? safeContains
                        : Expression.OrElse(combinedExpression, safeContains);
                }
            }
        }
        else
        {
            // Search all string properties
            var properties = typeof(T).GetProperties()
                .Where(prop => prop.PropertyType == typeof(string) ||
                              Nullable.GetUnderlyingType(prop.PropertyType) == typeof(string));

            foreach (var property in properties)
            {
                var propertyAccess = Expression.Property(parameter, property);
                var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

                var propertyToLower = Expression.Call(propertyAccess, toLowerMethod!);
                var keywordConstant = Expression.Constant(keyword);
                var containsCall = Expression.Call(propertyToLower, containsMethod!, keywordConstant);

                var nullCheck = Expression.NotEqual(propertyAccess, Expression.Constant(null, typeof(string)));
                var safeContains = Expression.AndAlso(nullCheck, containsCall);

                combinedExpression = combinedExpression == null
                    ? safeContains
                    : Expression.OrElse(combinedExpression, safeContains);
            }
        }

        if (combinedExpression != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    public static IQueryable<T> ApplyAdvancedFilter<T>(this IQueryable<T> query, Filter? filter)
    {
        if (filter is null)
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression binaryExpression;

        if (!string.IsNullOrEmpty(filter.Logic))
        {
            if (filter.Filters is null)
            {
                throw new InvalidOperationException("The Filters attribute is required when declaring a logic");
            }

            binaryExpression = CreateFilterExpression(filter.Logic, filter.Filters, parameter);
        }
        else
        {
            var validFilter = GetValidFilter(filter);
            binaryExpression = CreateFilterExpression(validFilter.Field!, validFilter.Operator!, validFilter.Value, parameter);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(binaryExpression, parameter);
        return query.Where(lambda);
    }

    public static IQueryable<T> ApplyOrdering<T>(this IQueryable<T> query, string[] orderByFields)
    {
        if (orderByFields is null || !orderByFields.Any())
        {
            return query;
        }

        IOrderedQueryable<T>? orderedQuery = null;
        bool isFirst = true;

        foreach (var field in orderByFields)
        {
            var parts = field.Split(' ');
            var propertyName = parts[0];
            var descending = parts.Length > 1 && parts[1].StartsWith("Desc", StringComparison.OrdinalIgnoreCase);

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression property = parameter;
            
            // Handle nested properties (e.g., "User.Name")
            foreach (var member in propertyName.Split('.'))
            {
                property = Expression.PropertyOrField(property, member);
            }
            
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), property.Type);
            var lambda = Expression.Lambda(delegateType, property, parameter);

            var methodName = isFirst
                ? (descending ? "OrderByDescending" : "OrderBy")
                : (descending ? "ThenByDescending" : "ThenBy");

            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), property.Type);

            orderedQuery = (IOrderedQueryable<T>)method.Invoke(null, new object[] { isFirst ? query : orderedQuery!, lambda })!;
            isFirst = false;
        }

        return orderedQuery ?? query;
    }

    #region Private Helper Methods

    private static Expression CreateFilterExpression(
        string logic,
        IEnumerable<Filter> filters,
        ParameterExpression parameter)
    {
        Expression? filterExpression = null;

        foreach (var filter in filters)
        {
            Expression bExpression;

            if (!string.IsNullOrEmpty(filter.Logic))
            {
                if (filter.Filters is null)
                {
                    throw new InvalidOperationException("The Filters attribute is required when declaring a logic");
                }

                bExpression = CreateFilterExpression(filter.Logic, filter.Filters, parameter);
            }
            else
            {
                var validFilter = GetValidFilter(filter);
                bExpression = CreateFilterExpression(validFilter.Field!, validFilter.Operator!, validFilter.Value, parameter);
            }

            filterExpression = filterExpression is null 
                ? bExpression 
                : CombineFilter(logic, filterExpression, bExpression);
        }

        return filterExpression ?? Expression.Constant(true);
    }

    private static Expression CreateFilterExpression(
        string field,
        string filterOperator,
        object? value,
        ParameterExpression parameter)
    {
        var propertyExpression = GetPropertyExpression(field, parameter);
        var valueExpression = GetValueExpression(field, value, propertyExpression.Type);
        return CreateFilterExpression(propertyExpression, valueExpression, filterOperator);
    }

    private static Expression CreateFilterExpression(
        Expression memberExpression,
        Expression constantExpression,
        string filterOperator)
    {
        if (memberExpression.Type == typeof(string))
        {
            var nullCheck = Expression.NotEqual(memberExpression, Expression.Constant(null, typeof(string)));
            
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            var memberToLower = Expression.Call(memberExpression, toLowerMethod!);
            
            if (constantExpression.Type == typeof(string))
            {
                constantExpression = Expression.Call(constantExpression, toLowerMethod!);
            }

            Expression operatorExpression = filterOperator switch
            {
                FilterOperator.EQ => Expression.Equal(memberToLower, constantExpression),
                FilterOperator.NEQ => Expression.NotEqual(memberToLower, constantExpression),
                FilterOperator.CONTAINS => Expression.Call(memberToLower, "Contains", null, constantExpression),
                FilterOperator.STARTSWITH => Expression.Call(memberToLower, "StartsWith", null, constantExpression),
                FilterOperator.ENDSWITH => Expression.Call(memberToLower, "EndsWith", null, constantExpression),
                _ => throw new InvalidOperationException($"Filter Operator '{filterOperator}' is not valid for string type."),
            };

            return Expression.AndAlso(nullCheck, operatorExpression);
        }

        return filterOperator switch
        {
            FilterOperator.EQ => Expression.Equal(memberExpression, constantExpression),
            FilterOperator.NEQ => Expression.NotEqual(memberExpression, constantExpression),
            FilterOperator.LT => Expression.LessThan(memberExpression, constantExpression),
            FilterOperator.LTE => Expression.LessThanOrEqual(memberExpression, constantExpression),
            FilterOperator.GT => Expression.GreaterThan(memberExpression, constantExpression),
            FilterOperator.GTE => Expression.GreaterThanOrEqual(memberExpression, constantExpression),
            _ => throw new InvalidOperationException($"Filter Operator '{filterOperator}' is not valid."),
        };
    }

    private static Expression CombineFilter(
        string filterOperator,
        Expression bExpressionBase,
        Expression bExpression) => filterOperator switch
        {
            FilterLogic.AND => Expression.AndAlso(bExpressionBase, bExpression),
            FilterLogic.OR => Expression.OrElse(bExpressionBase, bExpression),
            FilterLogic.XOR => Expression.ExclusiveOr(bExpressionBase, bExpression),
            _ => throw new ArgumentException($"FilterLogic '{filterOperator}' is not valid."),
        };

    private static Expression GetPropertyExpression(
        string propertyName,
        ParameterExpression parameter)
    {
        Expression propertyExpression = parameter;
        foreach (string member in propertyName.Split('.'))
        {
            propertyExpression = Expression.PropertyOrField(propertyExpression, member);
        }

        return propertyExpression;
    }

    private static string GetStringFromJsonElement(object value)
        => ((JsonElement)value).GetString()!;

    private static ConstantExpression GetValueExpression(
        string field,
        object? value,
        Type propertyType)
    {
        if (value == null)
        {
            return Expression.Constant(null, propertyType);
        }

        if (propertyType.IsEnum)
        {
            string? stringEnum = GetStringFromJsonElement(value);

            if (!Enum.TryParse(propertyType, stringEnum, true, out object? valueParsed))
            {
                throw new InvalidOperationException($"Value '{value}' is not valid for field '{field}'");
            }

            return Expression.Constant(valueParsed, propertyType);
        }

        if (propertyType == typeof(Guid) || propertyType == typeof(Guid?))
        {
            string? stringGuid = GetStringFromJsonElement(value);

            if (!Guid.TryParse(stringGuid, out Guid valueParsed))
            {
                throw new InvalidOperationException($"Value '{value}' is not valid for field '{field}'");
            }

            return Expression.Constant(valueParsed, propertyType);
        }

        if (propertyType == typeof(string))
        {
            string? text = GetStringFromJsonElement(value);
            return Expression.Constant(text, propertyType);
        }

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            string? text = GetStringFromJsonElement(value);
            return Expression.Constant(ChangeType(text, propertyType), propertyType);
        }

        return Expression.Constant(ChangeType(((JsonElement)value).GetRawText(), propertyType), propertyType);
    }

    private static dynamic? ChangeType(object value, Type conversion)
    {
        var t = conversion;

        if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (value == null)
            {
                return null;
            }

            t = Nullable.GetUnderlyingType(t);
        }

        return Convert.ChangeType(value, t!);
    }

    private static Filter GetValidFilter(Filter filter)
    {
        if (string.IsNullOrEmpty(filter.Field))
        {
            throw new InvalidOperationException("The Field attribute is required when declaring a filter");
        }

        if (string.IsNullOrEmpty(filter.Operator))
        {
            throw new InvalidOperationException("The Operator attribute is required when declaring a filter");
        }

        return filter;
    }

    #endregion
}



