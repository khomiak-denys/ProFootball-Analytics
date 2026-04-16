namespace ProFootball.Application.Contracts.Queries;

public sealed record PlayerListItemDto(
    int PlayerApiId,
    string Name,
    DateTime? Birthday,
    int? Height,
    int? Weight,
    int? OverallRating,
    int? Potential,
    string? PreferredFoot);
