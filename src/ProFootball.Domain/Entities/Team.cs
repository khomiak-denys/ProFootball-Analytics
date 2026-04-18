namespace ProFootball.Domain.Entities;

public sealed class Team
{
    private Team()
    {
    }

    public Team(int id, int teamApiId, int? teamFifaApiId, string longName, string? shortName)
    {
        if (longName is null)
        {
            throw new ArgumentNullException(nameof(longName), "Long name cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(longName))
        {
            throw new ArgumentException("Long name cannot be null or whitespace.", nameof(longName));
        }

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
