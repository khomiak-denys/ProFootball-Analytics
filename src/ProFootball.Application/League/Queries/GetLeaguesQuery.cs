using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.League.Dtos;

namespace ProFootball.Application.League.Queries;

public sealed record GetLeaguesQuery(int? CountryId = null) : IQuery<IReadOnlyList<LeagueDto>>;
