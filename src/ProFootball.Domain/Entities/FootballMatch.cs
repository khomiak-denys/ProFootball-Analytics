namespace ProFootball.Domain.Entities;

public sealed class FootballMatch
{
    private FootballMatch()
    {
    }

    public FootballMatch(
        int id,
        string countryName,
        int leagueId,
        string season,
        DateTime date,
        int matchApiId,
        int homeTeamApiId,
        int awayTeamApiId,
        int? homeTeamGoal,
        int? awayTeamGoal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(countryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(season);

        Id = id;
        CountryName = countryName.Trim();
        LeagueId = leagueId;
        Season = season.Trim();
        Date = date;
        MatchApiId = matchApiId;
        HomeTeamApiId = homeTeamApiId;
        AwayTeamApiId = awayTeamApiId;
        HomeTeamGoal = homeTeamGoal;
        AwayTeamGoal = awayTeamGoal;
    }

    public int Id { get; private set; }

    public string CountryName { get; private set; } = string.Empty;

    public int LeagueId { get; private set; }

    public string Season { get; private set; } = string.Empty;

    public DateTime Date { get; private set; }

    public int MatchApiId { get; private set; }

    public int HomeTeamApiId { get; private set; }

    public int AwayTeamApiId { get; private set; }

    public int? HomeTeamGoal { get; private set; }

    public int? AwayTeamGoal { get; private set; }

    public League? League { get; private set; }
}
