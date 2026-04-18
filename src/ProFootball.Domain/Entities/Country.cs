namespace ProFootball.Domain.Entities;

public sealed class Country
{
    private Country()
    {
    }

    public Country(int id, string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name), "Name cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
