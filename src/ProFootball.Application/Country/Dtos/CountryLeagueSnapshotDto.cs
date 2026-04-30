using ProFootball.Application.League.Dtos;

namespace ProFootball.Application.Country.Dtos;

public sealed record CountryLeagueSnapshotDto(
    IReadOnlyList<LeagueCountryCardDto> LeagueCards,
    CountryLeagueSummaryDto Summary,
    string AboutDescription);
