using System.Linq.Expressions;

namespace ReadersRealm.Api.Data;

public static class QueryableSorting
{
    public static IOrderedQueryable<T> SortBy<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> key,
        bool descending
    ) => descending ? query.OrderByDescending(key) : query.OrderBy(key);

    public static IOrderedQueryable<T> ThenSortBy<T, TKey>(
        this IOrderedQueryable<T> query,
        Expression<Func<T, TKey>> key,
        bool descending
    ) => descending ? query.ThenByDescending(key) : query.ThenBy(key);
}
