namespace ProFootball.Domain.Entities;

public sealed class TeamAttribute
{
    private TeamAttribute()
    {
    }

    public TeamAttribute(
        int id,
        int teamApiId,
        int? teamFifaApiId,
        DateTime date,
        int? buildUpPlaySpeed,
        int? buildUpPlayPassing,
        int? chanceCreationPassing,
        int? defencePressure)
    {
        Id = id;
        TeamApiId = teamApiId;
        TeamFifaApiId = teamFifaApiId;
        Date = date;
        BuildUpPlaySpeed = buildUpPlaySpeed;
        BuildUpPlayPassing = buildUpPlayPassing;
        ChanceCreationPassing = chanceCreationPassing;
        DefencePressure = defencePressure;
    }

    public int Id { get; private set; }

    public int TeamApiId { get; private set; }

    public int? TeamFifaApiId { get; private set; }

    public DateTime Date { get; private set; }

    public int? BuildUpPlaySpeed { get; private set; }

    public int? BuildUpPlayPassing { get; private set; }

    public int? ChanceCreationPassing { get; private set; }

    public int? DefencePressure { get; private set; }

    public Team? Team { get; private set; }
}
