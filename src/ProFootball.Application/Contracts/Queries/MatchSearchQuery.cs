namespace ProFootball.Application.Contracts.Queries;

public sealed record MatchSearchQuery(
    int? LeagueId,
    string? Season,
    int? TeamApiId,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? SortBy,
    bool SortDescending,
    int Page = 1,
    int PageSize = 20);
