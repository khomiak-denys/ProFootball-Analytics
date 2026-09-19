using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;

namespace ProFootball.Application.Team.Commands;

public sealed record CreateTeamCommand(string Name, string? ShortName, int? LeagueId) : ICommand<Result>;
