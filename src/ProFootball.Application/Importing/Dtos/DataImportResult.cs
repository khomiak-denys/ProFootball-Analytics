namespace ProFootball.Application.Importing.Dtos;

public sealed record DataImportResult(
    int CountriesResolved,
    int LeaguesImported,
    int TeamsImported,
    int PlayersImported,
    int MatchesImported,
    int TeamAttributesImported,
    int PlayerAttributesImported,
    int SkippedRows,
    TimeSpan Duration);
