namespace ProFootball.Application.Player.Dtos;

public sealed record PlayerListItemDto(
    int PlayerApiId,
    string Name,
    DateTime? Birthday,
    int? Height,
    int? Weight,
    int? OverallRating,
    int? Potential,
    string? PreferredFoot);
