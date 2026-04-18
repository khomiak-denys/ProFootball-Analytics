namespace ProFootball.Application.Player.Dtos;

public sealed record PlayerAttributeDto(
    DateTime Date,
    int? OverallRating,
    int? Potential,
    string? PreferredFoot,
    string? AttackingWorkRate,
    string? DefensiveWorkRate);
