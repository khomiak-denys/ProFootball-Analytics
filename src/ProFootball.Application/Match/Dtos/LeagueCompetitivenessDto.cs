namespace ProFootball.Application.Match.Dtos;

public sealed record LeagueCompetitivenessDto(
    int LeagueId,
    string LeagueName,
    string CountryName,
    int MatchCount,
    double DrawRate,
    double AverageGoalDifference);
