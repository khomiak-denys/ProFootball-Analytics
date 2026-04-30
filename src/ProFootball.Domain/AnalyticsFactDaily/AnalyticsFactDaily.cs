namespace ProFootball.Domain.Entities;

public sealed class AnalyticsFactDaily
{
    private AnalyticsFactDaily()
    {
    }

    public AnalyticsFactDaily(
        long id,
        DateOnly dateKey,
        string season,
        int leagueId,
        string metricKey,
        decimal metricValue,
        DateTime updatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(season);
        ArgumentException.ThrowIfNullOrWhiteSpace(metricKey);

        Id = id;
        DateKey = dateKey;
        Season = season.Trim();
        LeagueId = leagueId;
        MetricKey = metricKey.Trim();
        MetricValue = metricValue;
        UpdatedAtUtc = updatedAtUtc;
    }

    public long Id { get; private set; }

    public DateOnly DateKey { get; private set; }

    public string Season { get; private set; } = string.Empty;

    public int LeagueId { get; private set; }

    public string MetricKey { get; private set; } = string.Empty;

    public decimal MetricValue { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }
}

