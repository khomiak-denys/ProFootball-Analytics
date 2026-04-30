namespace ProFootball.Application.Match.Dtos;

public sealed record MatchOutcomeDistributionDto(
    int HomeWins,
    int Draws,
    int AwayWins,
    double AverageGoalsPerMatch,
    int MatchCount);
