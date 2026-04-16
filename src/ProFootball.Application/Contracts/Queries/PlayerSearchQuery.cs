namespace ProFootball.Application.Contracts.Queries;

public sealed record PlayerSearchQuery(
    string? Name,
    int? MinOverallRating,
    int? MaxOverallRating,
    int? MinPotential,
    int? MaxPotential,
    int? MinHeight,
    int? MaxHeight,
    string? PreferredFoot,
    string? SortBy,
    bool SortDescending,
    int Page = 1,
    int PageSize = 20);
