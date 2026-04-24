namespace ProFootball.Domain.Entities;

public sealed class Team
{
    private Team()
    {
    }

    public Team(int id, string longName, string? shortName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(longName);

        Id = id;
        LongName = longName.Trim();
        ShortName = shortName?.Trim();
    }

    public int Id { get; private set; }

    public string LongName { get; private set; } = string.Empty;

    public string? ShortName { get; private set; }
}
