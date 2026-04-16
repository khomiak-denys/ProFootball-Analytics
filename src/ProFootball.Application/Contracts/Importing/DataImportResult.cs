namespace ProFootball.Application.Contracts.Importing;

public sealed record DataImportResult(
    int CountriesImported,
    int LeaguesImported,
    int TeamsImported,
    int PlayersImported,
    int MatchesImported,
    int TeamAttributesImported,
    int PlayerAttributesImported,
    int SkippedRows,
    TimeSpan Duration);
