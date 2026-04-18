namespace ProFootball.Application.Player.Dtos;

public sealed record PlayerDetailsDto(
    int PlayerApiId,
    int? PlayerFifaApiId,
    string Name,
    DateTime? Birthday,
    int? Height,
    int? Weight,
    IReadOnlyList<PlayerAttributeDto> Attributes);
