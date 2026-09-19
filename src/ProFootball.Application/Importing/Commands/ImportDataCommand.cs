using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Importing.Dtos;

namespace ProFootball.Application.Importing.Commands;

public sealed record ImportDataCommand(
    string SqlitePath,
    int BatchSize = 1_000,
    string Profile = "realistic",
    string Mode = "regenerate") : ICommand<DataImportResult>;
