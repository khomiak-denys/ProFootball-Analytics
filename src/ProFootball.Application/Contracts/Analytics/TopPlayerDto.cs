namespace ProFootball.Application.Contracts.Analytics;

public sealed record TopPlayerDto(
    int PlayerApiId,
    string PlayerName,
    double AverageOverallRating,
    double AveragePotential,
    int Samples);
