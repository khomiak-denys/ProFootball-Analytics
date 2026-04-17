namespace ProFootball.Application.Contracts.Queries;

public sealed record CountryLeagueSnapshotDto(
    IReadOnlyList<LeagueCountryCardDto> LeagueCards,
    CountryLeagueSummaryDto Summary);
