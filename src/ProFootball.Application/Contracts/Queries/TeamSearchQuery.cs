namespace ProFootball.Application.Contracts.Queries;

public sealed record TeamSearchQuery(
    string? Name,
    string? SortBy,
    bool SortDescending,
    int Page = 1,
    int PageSize = 20);
