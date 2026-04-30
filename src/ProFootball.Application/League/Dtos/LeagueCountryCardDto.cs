namespace ProFootball.Application.League.Dtos;

public sealed record LeagueCountryCardDto(
    int LeagueId,
    string LeagueName,
    string Season,
    int TeamsCount,
    int MatchCount,
    int? MaxTeams,
    string? Description);
