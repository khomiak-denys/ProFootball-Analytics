using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Team.Dtos;

namespace ProFootball.Application.Team.Queries;

public sealed record GetTeamDetailsQuery(int TeamApiId) : IQuery<TeamDetailsDto?>;
