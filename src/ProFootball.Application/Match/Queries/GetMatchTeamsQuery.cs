using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Team.Dtos;

namespace ProFootball.Application.Match.Queries;

public sealed record GetMatchTeamsQuery(int? LeagueId = null) : IQuery<IReadOnlyList<TeamListItemDto>>;
