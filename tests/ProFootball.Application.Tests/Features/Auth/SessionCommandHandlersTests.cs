using ProFootball.Application.Auth.Commands;
using ProFootball.Application.Auth.Handlers;
using ProFootball.Application.Auth.Queries;
using Xunit;

namespace ProFootball.Application.Tests.Features.Auth;

public class SessionCommandHandlersTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SignIn_ShouldThrow_WhenUserNameIsMissing(string? userName)
    {
        var store = new InMemorySessionStore();
        var handler = new SignInCommandHandler(store);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => handler.HandleAsync(new SignInCommand(userName!)));
    }

    [Fact]
    public async Task SignIn_ShouldSetAuthenticatedUser()
    {
        var store = new InMemorySessionStore();
        var signInHandler = new SignInCommandHandler(store);
        var stateHandler = new GetSessionStateQueryHandler(store);

        await signInHandler.HandleAsync(new SignInCommand("  manager  "));
        var state = await stateHandler.HandleAsync(new GetSessionStateQuery());

        Assert.True(state.IsAuthenticated);
        Assert.Equal("manager", state.CurrentUser);
    }

    [Fact]
    public async Task SignOut_ShouldClearSession()
    {
        var store = new InMemorySessionStore();
        var signInHandler = new SignInCommandHandler(store);
        var signOutHandler = new SignOutCommandHandler(store);
        var stateHandler = new GetSessionStateQueryHandler(store);

        await signInHandler.HandleAsync(new SignInCommand("coach"));
        await signOutHandler.HandleAsync(new SignOutCommand());

        var state = await stateHandler.HandleAsync(new GetSessionStateQuery());
        Assert.False(state.IsAuthenticated);
        Assert.Null(state.CurrentUser);
    }

    [Fact]
    public async Task SignOut_ShouldBeIdempotent_WhenSessionIsAlreadyCleared()
    {
        var store = new InMemorySessionStore();
        var signOutHandler = new SignOutCommandHandler(store);
        var stateHandler = new GetSessionStateQueryHandler(store);

        await signOutHandler.HandleAsync(new SignOutCommand());
        await signOutHandler.HandleAsync(new SignOutCommand());

        var state = await stateHandler.HandleAsync(new GetSessionStateQuery());
        Assert.False(state.IsAuthenticated);
        Assert.Null(state.CurrentUser);
    }
}
