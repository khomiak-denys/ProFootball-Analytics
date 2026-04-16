namespace ProFootball.Application.Contracts.Queries;

public sealed record PlayerDetailsDto(
    int PlayerApiId,
    int? PlayerFifaApiId,
    string Name,
    DateTime? Birthday,
    int? Height,
    int? Weight,
    IReadOnlyList<PlayerAttributeDto> Attributes);
