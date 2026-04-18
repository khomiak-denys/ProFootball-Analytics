using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Player.Dtos;

namespace ProFootball.Application.Player.Queries;

public sealed record TopPlayersQuery(
    int Limit = 20,
    int? MinOverallRating = null,
    int? MinPotential = null,
    string? PreferredFoot = null,
    string? SortBy = null,
    bool SortDescending = true) : IQuery<IReadOnlyList<TopPlayerDto>>;
