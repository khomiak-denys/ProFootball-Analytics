namespace ProFootball.Domain.Entities;

public sealed class League
{
    private League()
    {
    }

    public League(int id, int countryId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        CountryId = countryId;
        Name = name.Trim();
    }

    public int Id { get; private set; }

    public int CountryId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Country? Country { get; private set; }
}
