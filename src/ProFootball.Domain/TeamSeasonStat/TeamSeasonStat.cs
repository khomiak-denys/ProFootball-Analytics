namespace ProFootball.Domain.Entities;

public sealed class TeamSeasonStat
{
    private TeamSeasonStat()
    {
    }

    public TeamSeasonStat(
        long id,
        string season,
        int leagueId,
        int teamId,
        int matches,
        int wins,
        int draws,
        int losses,
        int goalsFor,
        int goalsAgainst,
        int points)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(season);

        Id = id;
        Season = season.Trim();
        LeagueId = leagueId;
        TeamId = teamId;
        Matches = matches;
        Wins = wins;
        Draws = draws;
        Losses = losses;
        GoalsFor = goalsFor;
        GoalsAgainst = goalsAgainst;
        Points = points;
    }

    public long Id { get; private set; }

    public string Season { get; private set; } = string.Empty;

    public int LeagueId { get; private set; }

    public int TeamId { get; private set; }

    public int Matches { get; private set; }

    public int Wins { get; private set; }

    public int Draws { get; private set; }

    public int Losses { get; private set; }

    public int GoalsFor { get; private set; }

    public int GoalsAgainst { get; private set; }

    public int Points { get; private set; }
}

