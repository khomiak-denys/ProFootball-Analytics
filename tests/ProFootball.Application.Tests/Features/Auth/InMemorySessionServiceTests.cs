using ProFootball.Application.Services.Auth;
using Xunit;

namespace ProFootball.Application.Tests.Features.Auth;

public class InMemorySessionServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SignIn_ShouldThrow_WhenUserNameIsMissing(string? userName)
    {
        var session = new InMemorySessionService();

        Assert.Throws<ArgumentException>(() => session.SignIn(userName!));
    }

    [Fact]
    public void SignIn_ShouldSetAuthenticatedUser()
    {
        var session = new InMemorySessionService();

        session.SignIn("  manager  ");

        Assert.True(session.IsAuthenticated);
        Assert.Equal("manager", session.CurrentUser);
    }

    [Fact]
    public void SignOut_ShouldClearSession()
    {
        var session = new InMemorySessionService();
        session.SignIn("coach");

        session.SignOut();

        Assert.False(session.IsAuthenticated);
        Assert.Null(session.CurrentUser);
    }

    [Fact]
    public void SignOut_ShouldBeIdempotent_WhenSessionIsAlreadyCleared()
    {
        var session = new InMemorySessionService();
        session.SignOut();
        session.SignOut();

        Assert.False(session.IsAuthenticated);
        Assert.Null(session.CurrentUser);
    }
}
