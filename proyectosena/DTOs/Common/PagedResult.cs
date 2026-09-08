// =============================================
// DTO: PagedResult<T>
// Wraps any list response with its paging information,
// so the client knows whether there is more to ask for.
// =============================================

namespace proyectosena.DTOs.Common
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        // Default page size when the caller does not ask for one
        public const int DefaultPageSize = 20;

        // Upper bound, so nobody can request the whole table with pageSize=999999
        public const int MaxPageSize = 100;

        public static PagedResult<T> Create(List<T> items, int page, int pageSize, int totalItems)
        {
            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 0
            };
        }

        // Clamps whatever the caller sent into a sane range.
        // page < 1 becomes 1; pageSize outside 1..MaxPageSize is corrected.
        public static (int Page, int PageSize) Normalize(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = DefaultPageSize;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;
            return (page, pageSize);
        }
    }
}
