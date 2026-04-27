namespace ProFootball.Domain.Entities;

public sealed class TeamAttribute
{
    private TeamAttribute()
    {
    }

    public TeamAttribute(
        int id,
        int teamId,
        DateTime date,
        int? buildUpPlaySpeed,
        int? buildUpPlayPassing,
        int? chanceCreationPassing,
        int? defencePressure)
    {
        Id = id;
        TeamId = teamId;
        Date = date;
        BuildUpPlaySpeed = buildUpPlaySpeed;
        BuildUpPlayPassing = buildUpPlayPassing;
        ChanceCreationPassing = chanceCreationPassing;
        DefencePressure = defencePressure;
    }

    public int Id { get; private set; }

    public int TeamId { get; private set; }

    public DateTime Date { get; private set; }

    public int? BuildUpPlaySpeed { get; private set; }

    public int? BuildUpPlayPassing { get; private set; }

    public int? ChanceCreationPassing { get; private set; }

    public int? DefencePressure { get; private set; }

    public Team? Team { get; private set; }
}
