namespace ProFootball.Application.Contracts.Importing;

/// <param name="SqlitePath">Path to source SQLite database file.</param>
/// <param name="BatchSize">Number of rows saved per EF batch. Must be greater than zero.</param>
public sealed record DataImportRequest(string SqlitePath, int BatchSize = 1_000);
