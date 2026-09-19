namespace ProFootball.Application.Player.Dtos;

public sealed record PlayerRatingDeltaDto(
    int PlayerId,
    string PlayerName,
    int OverallRating,
    int Potential,
    int Delta,
    DateTime AttributeDate);
