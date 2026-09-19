namespace ProFootball.Infrastructure.Querying.ReadModels;

public sealed class PlayerRatingTrendMonthlyReadModel
{
    public string Season { get; init; } = string.Empty;

    public int LeagueId { get; init; }

    public DateTime MonthStart { get; init; }

    public decimal AvgOverallRating { get; init; }
}

