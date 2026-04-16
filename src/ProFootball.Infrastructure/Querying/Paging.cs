namespace ProFootball.Infrastructure.Querying;

internal static class Paging
{
    public static (int Page, int PageSize, int Skip) Normalize(int page, int pageSize, int maxPageSize = 200)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 20 : pageSize;
        if (normalizedPageSize > maxPageSize)
        {
            normalizedPageSize = maxPageSize;
        }

        var skip = ((long)normalizedPage - 1L) * normalizedPageSize;
        if (skip > int.MaxValue)
        {
            skip = int.MaxValue;
        }

        return (normalizedPage, normalizedPageSize, (int)skip);
    }
}
