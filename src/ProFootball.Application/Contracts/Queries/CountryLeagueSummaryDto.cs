namespace ProFootball.Application.Contracts.Queries;

public sealed record CountryLeagueSummaryDto(
    int TotalClubs,
    int ActiveLeagues,
    int Divisions);
