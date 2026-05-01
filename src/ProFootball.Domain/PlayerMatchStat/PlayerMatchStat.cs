namespace ProFootball.Domain.Entities;

public sealed class PlayerMatchStat
{
    private PlayerMatchStat()
    {
    }

    public PlayerMatchStat(
        long id,
        int matchId,
        int playerId,
        int teamId,
        short minutes,
        short shots,
        short passes,
        short tackles,
        short goals,
        short assists,
        decimal xg)
    {
        Id = id;
        MatchId = matchId;
        PlayerId = playerId;
        TeamId = teamId;
        Minutes = minutes;
        Shots = shots;
        Passes = passes;
        Tackles = tackles;
        Goals = goals;
        Assists = assists;
        Xg = xg;
    }

    public long Id { get; private set; }

    public int MatchId { get; private set; }

    public int PlayerId { get; private set; }

    public int TeamId { get; private set; }

    public short Minutes { get; private set; }

    public short Shots { get; private set; }

    public short Passes { get; private set; }

    public short Tackles { get; private set; }

    public short Goals { get; private set; }

    public short Assists { get; private set; }

    public decimal Xg { get; private set; }
}

