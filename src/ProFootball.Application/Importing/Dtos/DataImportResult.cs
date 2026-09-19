namespace ProFootball.Application.Importing.Dtos;

public sealed record DataImportResult(
    int CountriesResolved,
    int LeaguesImported,
    int TeamsImported,
    int PlayersImported,
    int MatchesImported,
    int TeamAttributesImported,
    int PlayerAttributesImported,
    int MatchEventsGenerated,
    int PlayerMatchStatsGenerated,
    int TeamSeasonStatsRebuilt,
    int ValidationErrors,
    int SkippedRows,
    TimeSpan Duration);
