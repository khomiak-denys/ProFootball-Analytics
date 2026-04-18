namespace ProFootball.Domain.Entities;

public sealed class FootballClub
{
    private FootballClub()
    {
    }

    public FootballClub(string name, int foundedYear)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name), "Name cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
        }

        if (foundedYear < 1857 || foundedYear > DateTime.UtcNow.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(foundedYear), "Founded year is out of valid range.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        FoundedYear = foundedYear;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int FoundedYear { get; private set; }
}
