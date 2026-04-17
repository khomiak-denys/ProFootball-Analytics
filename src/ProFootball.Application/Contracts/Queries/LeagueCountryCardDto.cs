namespace ProFootball.Application.Contracts.Queries;

public sealed record LeagueCountryCardDto(
    int LeagueId,
    string LeagueName,
    string Season,
    int TeamsCount,
    int MatchesCount);
