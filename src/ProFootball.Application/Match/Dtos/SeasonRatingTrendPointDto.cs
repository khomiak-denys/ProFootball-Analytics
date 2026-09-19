using System;

namespace ProFootball.Application.Match.Dtos;

public sealed record SeasonRatingTrendPointDto(
    DateTime Month,
    double AverageOverallRating);
