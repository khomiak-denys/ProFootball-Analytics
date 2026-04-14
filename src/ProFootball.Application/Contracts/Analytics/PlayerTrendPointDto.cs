namespace ProFootball.Application.Contracts.Analytics;

public sealed record PlayerTrendPointDto(DateTime Date, int? OverallRating, int? Potential);
