using ProFootball.Domain.Entities;
using Xunit;

namespace ProFootball.Domain.Tests.Features.Entities;

public class DomainEntitiesTests
{
    [Fact]
    public void League_Constructor_ShouldThrowArgumentNullException_WhenCountryNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new League(2, null!, "Premier League"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void League_Constructor_ShouldThrowArgumentException_WhenCountryNameIsWhitespace(string countryName)
    {
        Assert.Throws<ArgumentException>(() => new League(2, countryName, "Premier League"));
    }

    [Fact]
    public void League_Constructor_ShouldThrowArgumentNullException_WhenNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new League(2, "England", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void League_Constructor_ShouldThrowArgumentException_WhenNameIsWhitespace(string name)
    {
        Assert.Throws<ArgumentException>(() => new League(2, "England", name));
    }

    [Fact]
    public void League_Constructor_ShouldSetFields()
    {
        var league = new League(2, " England ", "  Premier League ");

        Assert.Equal(2, league.Id);
        Assert.Equal("England", league.CountryName);
        Assert.Equal("Premier League", league.Name);
    }

    [Fact]
    public void Team_Constructor_ShouldThrowArgumentNullException_WhenLongNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Team(10, 1001, 2001, null!, "DYK"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Team_Constructor_ShouldThrowArgumentException_WhenLongNameIsWhitespace(string longName)
    {
        Assert.Throws<ArgumentException>(() => new Team(10, 1001, 2001, longName, "DYK"));
    }

    [Fact]
    public void Team_Constructor_ShouldSetFieldsAndTrimNames()
    {
        var team = new Team(10, 1001, 2001, "  Dynamo Kyiv ", " DYK ");

        Assert.Equal(10, team.Id);
        Assert.Equal(1001, team.TeamApiId);
        Assert.Equal(2001, team.TeamFifaApiId);
        Assert.Equal("Dynamo Kyiv", team.LongName);
        Assert.Equal("DYK", team.ShortName);
    }

    [Fact]
    public void Player_Constructor_ShouldThrowArgumentNullException_WhenNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Player(11, 1101, 2101, null!, DateTime.UtcNow, 182, 78));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Player_Constructor_ShouldThrowArgumentException_WhenNameIsWhitespace(string name)
    {
        Assert.Throws<ArgumentException>(() => new Player(11, 1101, 2101, name, DateTime.UtcNow, 182, 78));
    }

    [Fact]
    public void Player_Constructor_ShouldSetFieldsAndTrimName()
    {
        var birthday = new DateTime(1993, 6, 24, 0, 0, 0, DateTimeKind.Utc);
        var player = new Player(11, 1101, 2101, "  Player Name  ", birthday, 182, 78);

        Assert.Equal(11, player.Id);
        Assert.Equal(1101, player.PlayerApiId);
        Assert.Equal(2101, player.PlayerFifaApiId);
        Assert.Equal("Player Name", player.Name);
        Assert.Equal(birthday, player.Birthday);
        Assert.Equal(182, player.Height);
        Assert.Equal(78, player.Weight);
    }

    [Fact]
    public void FootballMatch_Constructor_ShouldThrowArgumentNullException_WhenCountryNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FootballMatch(22, null!, 2, "2014/2015", DateTime.UtcNow, 33001, 100, 200, 2, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FootballMatch_Constructor_ShouldThrowArgumentException_WhenCountryNameIsWhitespace(string countryName)
    {
        Assert.Throws<ArgumentException>(() =>
            new FootballMatch(22, countryName, 2, "2014/2015", DateTime.UtcNow, 33001, 100, 200, 2, 1));
    }

    [Fact]
    public void FootballMatch_Constructor_ShouldThrowArgumentNullException_WhenSeasonIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FootballMatch(22, "England", 2, null!, DateTime.UtcNow, 33001, 100, 200, 2, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FootballMatch_Constructor_ShouldThrowArgumentException_WhenSeasonIsWhitespace(string season)
    {
        Assert.Throws<ArgumentException>(() =>
            new FootballMatch(22, "England", 2, season, DateTime.UtcNow, 33001, 100, 200, 2, 1));
    }

    [Fact]
    public void FootballMatch_Constructor_ShouldSetFieldsAndTrimSeason()
    {
        var date = new DateTime(2015, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var match = new FootballMatch(22, " England ", 2, " 2014/2015 ", date, 33001, 100, 200, 2, 1);

        Assert.Equal(22, match.Id);
        Assert.Equal("England", match.CountryName);
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

    [Fact]
    public void PlayerAttribute_Constructor_ShouldAllowNullOptionalTextFields()
    {
        var attribute = new PlayerAttribute(
            30,
            1101,
            2202,
            new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            70,
            78,
            null,
            null,
            null);

        Assert.Null(attribute.PreferredFoot);
        Assert.Null(attribute.AttackingWorkRate);
        Assert.Null(attribute.DefensiveWorkRate);
    }
}
