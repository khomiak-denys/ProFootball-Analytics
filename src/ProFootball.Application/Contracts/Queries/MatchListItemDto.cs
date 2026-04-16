namespace ProFootball.Application.Contracts.Queries;

public sealed record MatchListItemDto(
    int MatchApiId,
    DateTime Date,
    string Season,
    string LeagueName,
    string CountryName,
    int HomeTeamApiId,
    string HomeTeamName,
    int AwayTeamApiId,
    string AwayTeamName,
    int? HomeTeamGoal,
    int? AwayTeamGoal);
