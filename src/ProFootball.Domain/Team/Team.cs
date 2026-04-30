namespace ProFootball.Domain.Entities;

public sealed class Team
{
    private Team()
    {
    }

    public Team(int id, string longName, string? shortName, int? leagueId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(longName);

        Id = id;
        LongName = longName.Trim();
        ShortName = shortName?.Trim();
        LeagueId = leagueId;
    }

    public int Id { get; private set; }

    public string LongName { get; private set; } = string.Empty;

    public string? ShortName { get; private set; }

    public int? LeagueId { get; private set; }
}
