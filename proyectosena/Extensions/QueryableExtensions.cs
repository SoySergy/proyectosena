using Microsoft.EntityFrameworkCore;

namespace proyectosena.Extensions
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// Runs a query one page at a time. Two round trips: one COUNT and one
        /// SELECT with OFFSET/FETCH — never the whole table into memory.
        /// </summary>
        public static async Task<(List<T> Items, int Total)> ToPagedAsync<T>(
            this IQueryable<T> query, int page, int pageSize)
        {
            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}
