namespace ProFootball.Domain.Entities;

public sealed class PlayerAttribute
{
    private PlayerAttribute()
    {
    }

    public PlayerAttribute(
        int id,
        int playerId,
        DateTime date,
        int? overallRating,
        int? potential,
        string? preferredFoot,
        string? attackingWorkRate,
        string? defensiveWorkRate)
    {
        Id = id;
        PlayerId = playerId;
        Date = date;
        OverallRating = overallRating;
        Potential = potential;
        PreferredFoot = preferredFoot?.Trim();
        AttackingWorkRate = attackingWorkRate?.Trim();
        DefensiveWorkRate = defensiveWorkRate?.Trim();
    }

    public int Id { get; private set; }

    public int PlayerId { get; private set; }

    public DateTime Date { get; private set; }

    public int? OverallRating { get; private set; }

    public int? Potential { get; private set; }

    public string? PreferredFoot { get; private set; }

    public string? AttackingWorkRate { get; private set; }

    public string? DefensiveWorkRate { get; private set; }

    public Player? Player { get; private set; }
}
