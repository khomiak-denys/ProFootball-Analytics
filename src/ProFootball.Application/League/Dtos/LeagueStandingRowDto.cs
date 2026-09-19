namespace ProFootball.Application.League.Dtos;

public sealed record LeagueStandingRowDto(
    int Position,
    int TeamId,
    string TeamName,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    int Points);
