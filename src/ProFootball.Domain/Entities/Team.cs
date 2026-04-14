namespace ProFootball.Domain.Entities;

public sealed class Team
{
    private Team()
    {
    }

    public Team(int id, int teamApiId, int? teamFifaApiId, string longName, string? shortName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(longName);
        Id = id;
        TeamApiId = teamApiId;
        TeamFifaApiId = teamFifaApiId;
        LongName = longName.Trim();
        ShortName = shortName?.Trim();
    }

    public int Id { get; private set; }

    public int TeamApiId { get; private set; }

    public int? TeamFifaApiId { get; private set; }

    public string LongName { get; private set; } = string.Empty;

    public string? ShortName { get; private set; }
}
