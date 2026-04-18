namespace ProFootball.Domain.Entities;

public sealed class League
{
    private League()
    {
    }

    public League(int id, string countryName, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(countryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        CountryName = countryName.Trim();
        Name = name.Trim();
    }

    public int Id { get; private set; }

    public string CountryName { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
}
