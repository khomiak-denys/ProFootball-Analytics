namespace ProFootball.Domain.Entities;

public sealed class Player
{
    private Player()
    {
    }

    public Player(
        int id,
        string firstName,
        string lastName,
        DateTime? birthday,
        int? height,
        int? weight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        Id = id;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Birthday = birthday;
        Height = height;
        Weight = weight;
    }

    public int Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string FullName => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{FirstName} {LastName}").Trim();

    public DateTime? Birthday { get; private set; }

    public int? Height { get; private set; }

    public int? Weight { get; private set; }

    public void UpdateProfile(
        string firstName,
        string lastName,
        DateTime? birthday,
        int? height,
        int? weight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Birthday = birthday;
        Height = height;
        Weight = weight;
    }
}
