namespace ProFootball.Infrastructure.Querying.ReadModels;

public sealed class DashboardOverviewReadModel
{
    public string Season { get; init; } = string.Empty;

    public int LeagueId { get; init; }

    public string LeagueName { get; init; } = string.Empty;

    public string CountryName { get; init; } = string.Empty;

    public int MatchCount { get; init; }

    public int TotalClubs { get; init; }

    public int HomeWins { get; init; }

    public int Draws { get; init; }

    public int AwayWins { get; init; }

    public decimal AvgGoalsPerMatch { get; init; }

    public DateTime? LastMatchDate { get; init; }
}

