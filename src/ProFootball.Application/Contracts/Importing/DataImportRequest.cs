namespace ProFootball.Application.Contracts.Importing;

public sealed record DataImportRequest(string SqlitePath, int BatchSize = 1_000);
