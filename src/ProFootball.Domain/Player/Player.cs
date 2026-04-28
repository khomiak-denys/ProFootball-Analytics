namespace ProFootball.Domain.Entities;

public sealed class Player
{
    private Player()
    {
    }

    public Player(
        int id,
        string name,
        DateTime? birthday,
        int? height,
        int? weight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name.Trim();
        Birthday = birthday;
        Height = height;
        Weight = weight;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTime? Birthday { get; private set; }

    public int? Height { get; private set; }

    public int? Weight { get; private set; }

    public void UpdateProfile(
        string name,
        DateTime? birthday,
        int? height,
        int? weight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Birthday = birthday;
        Height = height;
        Weight = weight;
    }
}
