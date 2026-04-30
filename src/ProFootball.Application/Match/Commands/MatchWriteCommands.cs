using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;

namespace ProFootball.Application.Match.Commands;

public sealed record CreateMatchCommand(
    string CountryName,
    int LeagueId,
    string Season,
    DateTime Date,
    int HomeTeamId,
    int AwayTeamId,
    int? HomeTeamGoal,
    int? AwayTeamGoal) : ICommand<Result>;

public sealed record UpdateMatchCommand(
    int MatchId,
    string CountryName,
    int LeagueId,
    string Season,
    DateTime Date,
    int HomeTeamId,
    int AwayTeamId,
    int? HomeTeamGoal,
    int? AwayTeamGoal) : ICommand<Result>;

public sealed record DeleteMatchCommand(int MatchId) : ICommand<Result>;
