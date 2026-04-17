using ProFootball.Domain.Entities;
using Xunit;

namespace ProFootball.Domain.Tests.Features.Entities;

public class FootballClubTests
{
    [Fact]
    public void Constructor_ShouldSetValuesAndTrimName_WhenInputIsValid()
    {
        var club = new FootballClub("  Dynamo Kyiv  ", 1927);

        Assert.NotEqual(Guid.Empty, club.Id);
        Assert.Equal("Dynamo Kyiv", club.Name);
        Assert.Equal(1927, club.FoundedYear);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsMissing(string? name)
    {
        Assert.Throws<ArgumentException>(() => new FootballClub(name!, 1927));
    }

    [Theory]
    [InlineData(1200)]
    [InlineData(1856)]
    [InlineData(3000)]
    public void Constructor_ShouldThrow_WhenYearIsOutOfRange(int year)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FootballClub("Test", year));
    }
}
