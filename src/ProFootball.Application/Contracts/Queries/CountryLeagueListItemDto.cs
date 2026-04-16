namespace ProFootball.Application.Contracts.Queries;

public sealed record CountryLeagueListItemDto(
    int Id,
    string Name,
    int LeagueCount);
