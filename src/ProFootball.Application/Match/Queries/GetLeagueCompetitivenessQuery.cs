using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Match.Dtos;

namespace ProFootball.Application.Match.Queries;

public sealed record GetLeagueCompetitivenessQuery(
    string? Season,
    int Limit = 8) : IQuery<IReadOnlyList<LeagueCompetitivenessDto>>;
