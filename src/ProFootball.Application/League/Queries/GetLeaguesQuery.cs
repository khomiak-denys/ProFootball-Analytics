using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.League.Dtos;

namespace ProFootball.Application.League.Queries;

public sealed record GetLeaguesQuery(string? CountryName = null) : IQuery<IReadOnlyList<LeagueDto>>;
