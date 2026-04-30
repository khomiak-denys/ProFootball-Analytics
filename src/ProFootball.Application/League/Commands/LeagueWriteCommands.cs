using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;

namespace ProFootball.Application.League.Commands;

public sealed record CreateLeagueCommand(
    string CountryName,
    string Name,
    int? MaxTeams,
    string? Description) : ICommand<Result>;
