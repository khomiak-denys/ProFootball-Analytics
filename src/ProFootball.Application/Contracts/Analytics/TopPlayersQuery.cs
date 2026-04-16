namespace ProFootball.Application.Contracts.Analytics;

public sealed record TopPlayersQuery(
    int Limit = 20,
    int? MinOverallRating = null,
    int? MinPotential = null,
    string? PreferredFoot = null,
    string? SortBy = null,
    bool SortDescending = true);
