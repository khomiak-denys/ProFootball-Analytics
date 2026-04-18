namespace ProFootball.Application.Common;

public sealed record PagedResult<TItem>(
    IReadOnlyList<TItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
