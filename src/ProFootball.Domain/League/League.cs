namespace ProFootball.Domain.Entities;

public sealed class League
{
    private League()
    {
    }

    public League(
        int id,
        string countryName,
        string name,
        int? maxTeams = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(countryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        CountryName = countryName.Trim();
        Name = name.Trim();
        MaxTeams = maxTeams;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public int Id { get; private set; }

    public string CountryName { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public int? MaxTeams { get; private set; }

    public string? Description { get; private set; }
}
