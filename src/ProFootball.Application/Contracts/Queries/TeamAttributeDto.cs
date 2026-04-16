namespace ProFootball.Application.Contracts.Queries;

public sealed record TeamAttributeDto(
    DateTime Date,
    int? BuildUpPlaySpeed,
    int? BuildUpPlayPassing,
    int? ChanceCreationPassing,
    int? DefencePressure);
