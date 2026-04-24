using ProFootball.Application.Auth.Commands;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Handlers;
using ProFootball.Application.Auth.Queries;
using ProFootball.Domain.Entities;
using Xunit;

namespace ProFootball.Application.Tests.Features.Auth;

public class SessionCommandHandlersTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SignIn_ShouldThrow_WhenLoginIsMissing(string? login)
    {
        var store = new InMemorySessionStore();
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var handler = new SignInCommandHandler(store, repository, hasher);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => handler.HandleAsync(new SignInCommand(login!, "password123")));
    }

    [Fact]
    public async Task Register_ShouldAssignAdminRole_ToFirstUser()
    {
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);

        await registerHandler.HandleAsync(new RegisterUserCommand("Test", "Admin", "admin", "Password1", "Password1"));

        var user = await repository.FindByNormalizedLoginAsync("ADMIN");
        Assert.NotNull(user);
        Assert.Equal(AppUserRole.Admin, user!.Role);
        Assert.StartsWith("hashed:", user.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_ShouldAssignAnalystRole_ToSubsequentUsers()
    {
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);

        await registerHandler.HandleAsync(new RegisterUserCommand("First", "Admin", "admin", "Password1", "Password1"));
        await registerHandler.HandleAsync(new RegisterUserCommand("Second", "User", "analyst", "Password1", "Password1"));

        var user = await repository.FindByNormalizedLoginAsync("ANALYST");
        Assert.NotNull(user);
        Assert.Equal(AppUserRole.Analyst, user!.Role);
    }

    [Fact]
    public async Task SignIn_ShouldSetAuthenticatedUser_WhenCredentialsAreValid()
    {
        var store = new InMemorySessionStore();
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);
        var signInHandler = new SignInCommandHandler(store, repository, hasher);
        var stateHandler = new GetSessionStateQueryHandler(store);

        await registerHandler.HandleAsync(new RegisterUserCommand("John", "Manager", "manager", "Password1", "Password1"));
        await signInHandler.HandleAsync(new SignInCommand("manager", "Password1"));
        var state = await stateHandler.HandleAsync(new GetSessionStateQuery());

        Assert.True(state.IsAuthenticated);
        Assert.Equal("manager", state.Login);
        Assert.Equal("John Manager", state.DisplayName);
    }

    [Fact]
    public async Task SignOut_ShouldClearSession()
    {
        var store = new InMemorySessionStore();
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);
        var signInHandler = new SignInCommandHandler(store, repository, hasher);
        var signOutHandler = new SignOutCommandHandler(store);
        var stateHandler = new GetSessionStateQueryHandler(store);

        await registerHandler.HandleAsync(new RegisterUserCommand("Coach", "Person", "coach", "Password1", "Password1"));
        await signInHandler.HandleAsync(new SignInCommand("coach", "Password1"));
        await signOutHandler.HandleAsync(new SignOutCommand());

        var state = await stateHandler.HandleAsync(new GetSessionStateQuery());
        Assert.False(state.IsAuthenticated);
        Assert.Null(state.Login);
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
        Assert.Null(state.Login);
    }

    [Fact]
    public async Task SignIn_ShouldThrow_WhenPasswordIsInvalid()
    {
        var store = new InMemorySessionStore();
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);
        var signInHandler = new SignInCommandHandler(store, repository, hasher);

        await registerHandler.HandleAsync(new RegisterUserCommand("John", "Manager", "manager", "Password1", "Password1"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => signInHandler.HandleAsync(new SignInCommand("manager", "wrong-password")));
    }

    [Fact]
    public async Task Register_ShouldThrow_WhenLoginAlreadyExists()
    {
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);

        await registerHandler.HandleAsync(new RegisterUserCommand("First", "User", "manager", "Password1", "Password1"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            registerHandler.HandleAsync(new RegisterUserCommand("Second", "User", "manager", "Password1", "Password1")));
    }

    private sealed class InMemoryUserRepository : IAppUserAuthRepository
    {
        private readonly List<AppUser> _users = [];

        public Task<AppUser?> FindByNormalizedLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.SingleOrDefault(user => user.NormalizedLogin == normalizedLogin));

        public Task<bool> ExistsByNormalizedLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.Any(user => user.NormalizedLogin == normalizedLogin));

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_users.Count);

        public Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";

        public bool VerifyPassword(string password, string passwordHash)
            => string.Equals(passwordHash, HashPassword(password), StringComparison.Ordinal);
    }
}
