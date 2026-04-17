using ProFootball.Application.Services.Auth;
using Xunit;

namespace ProFootball.Application.Tests.Features.Auth;

public class InMemorySessionServiceTests
{
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
}
