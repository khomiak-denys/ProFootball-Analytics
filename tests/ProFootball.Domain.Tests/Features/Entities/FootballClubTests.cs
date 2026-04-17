using ProFootball.Domain.Entities;
using Xunit;

namespace ProFootball.Domain.Tests.Features.Entities;

public class FootballClubTests
{
    [Fact]
    public void Constructor_ShouldSetValues_WhenInputIsValid()
    {
        var club = new FootballClub("Dynamo Kyiv", 1927);

        Assert.NotEqual(Guid.Empty, club.Id);
        Assert.Equal("Dynamo Kyiv", club.Name);
        Assert.Equal(1927, club.FoundedYear);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenYearIsOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FootballClub("Test", 1200));
    }
}
