namespace ProFootball.Application.Player.Dtos;

public sealed record PlayerListItemDto(
    int PlayerApiId,
    string FirstName,
    string LastName,
    DateTime? Birthday,
    int? Height,
    int? Weight,
    int? OverallRating,
    int? Potential,
    string? PreferredFoot);
