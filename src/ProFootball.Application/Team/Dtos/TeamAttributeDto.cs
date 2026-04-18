namespace ProFootball.Application.Team.Dtos;

public sealed record TeamAttributeDto(
    DateTime Date,
    int? BuildUpPlaySpeed,
    int? BuildUpPlayPassing,
    int? ChanceCreationPassing,
    int? DefencePressure);
