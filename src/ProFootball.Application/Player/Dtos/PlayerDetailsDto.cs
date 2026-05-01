namespace ProFootball.Application.Player.Dtos;

public sealed record PlayerDetailsDto(
    int PlayerApiId,
    int? PlayerFifaApiId,
    string FirstName,
    string LastName,
    DateTime? Birthday,
    int? Height,
    int? Weight,
    IReadOnlyList<PlayerAttributeDto> Attributes);
