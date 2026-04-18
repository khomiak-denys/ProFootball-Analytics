namespace ProFootball.Application.Player.Dtos;

public sealed record TopPlayerDto(
    int PlayerApiId,
    string PlayerName,
    double AverageOverallRating,
    double AveragePotential,
    int Samples);
