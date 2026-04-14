using ProFootball.Domain.Entities;
using Xunit;

namespace ProFootball.Application.Tests;

public class DomainEntitiesTests
{
    [Fact]
    public void Country_Constructor_ShouldTrimName()
    {
        var country = new Country(1, "  Ukraine  ");

        Assert.Equal(1, country.Id);
        Assert.Equal("Ukraine", country.Name);
    }

    [Fact]
    public void League_Constructor_ShouldSetFields()
    {
        var league = new League(2, 1, "Premier League");

        Assert.Equal(2, league.Id);
        Assert.Equal(1, league.CountryId);
        Assert.Equal("Premier League", league.Name);
    }

    [Fact]
    public void Team_Constructor_ShouldSetFields()
    {
        var team = new Team(10, 1001, 2001, "Dynamo Kyiv", "DYK");

        Assert.Equal(10, team.Id);
        Assert.Equal(1001, team.TeamApiId);
        Assert.Equal(2001, team.TeamFifaApiId);
        Assert.Equal("Dynamo Kyiv", team.LongName);
        Assert.Equal("DYK", team.ShortName);
    }

    [Fact]
    public void Player_Constructor_ShouldSetFields()
    {
        var birthday = new DateTime(1993, 6, 24, 0, 0, 0, DateTimeKind.Utc);
        var player = new Player(11, 1101, 2101, "Player Name", birthday, 182, 78);

        Assert.Equal(11, player.Id);
        Assert.Equal(1101, player.PlayerApiId);
        Assert.Equal(2101, player.PlayerFifaApiId);
        Assert.Equal("Player Name", player.Name);
        Assert.Equal(birthday, player.Birthday);
        Assert.Equal(182, player.Height);
        Assert.Equal(78, player.Weight);
    }

    [Fact]
    public void FootballMatch_Constructor_ShouldSetFields()
    {
        var date = new DateTime(2015, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var match = new FootballMatch(22, 1, 2, "2014/2015", date, 33001, 100, 200, 2, 1);

        Assert.Equal(22, match.Id);
        Assert.Equal(1, match.CountryId);
        Assert.Equal(2, match.LeagueId);
        Assert.Equal("2014/2015", match.Season);
        Assert.Equal(date, match.Date);
        Assert.Equal(33001, match.MatchApiId);
        Assert.Equal(100, match.HomeTeamApiId);
        Assert.Equal(200, match.AwayTeamApiId);
        Assert.Equal(2, match.HomeTeamGoal);
        Assert.Equal(1, match.AwayTeamGoal);
    }

    [Fact]
    public void PlayerAttribute_Constructor_ShouldTrimTextFields()
    {
        var attribute = new PlayerAttribute(
            30,
            1101,
            2202,
            new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            70,
            78,
            " right ",
            " high ",
            " medium ");

        Assert.Equal("right", attribute.PreferredFoot);
        Assert.Equal("high", attribute.AttackingWorkRate);
        Assert.Equal("medium", attribute.DefensiveWorkRate);
    }
}
