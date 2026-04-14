namespace ProFootball.Domain.Entities;

public sealed class Country
{
    private Country()
    {
    }

    public Country(int id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name.Trim();
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
