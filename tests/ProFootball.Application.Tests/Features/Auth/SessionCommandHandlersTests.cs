using ProFootball.Application.Auth.Commands;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Dtos;
using ProFootball.Application.Auth.Handlers;
using ProFootball.Application.Auth.Queries;
using ProFootball.Application.Common;
using ProFootball.Domain.Entities;
using Xunit;

namespace ProFootball.Application.Tests.Features.Auth;

public class SessionCommandHandlersTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SignIn_ShouldFail_WhenLoginIsMissing(string? login)
    {
        var store = new InMemorySessionStore();
        var sessionPersistence = new InMemorySessionPersistence();
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var handler = new SignInCommandHandler(store, sessionPersistence, repository, hasher);

        var result = await handler.HandleAsync(new SignInCommand(login!, "password123"));
        Assert.True(result.IsFailure);
        Assert.Equal("auth.validation", result.Error?.Code);
    }

    [Fact]
    public async Task Register_ShouldAssignAdminRole_ToFirstUser()
    {
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);

        var registerResult = await registerHandler.HandleAsync(new RegisterUserCommand("Test", "Admin", "admin", "Password1", "Password1"));
        Assert.True(registerResult.IsSuccess);

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

        var first = await registerHandler.HandleAsync(new RegisterUserCommand("First", "Admin", "admin", "Password1", "Password1"));
        var second = await registerHandler.HandleAsync(new RegisterUserCommand("Second", "User", "analyst", "Password1", "Password1"));
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var user = await repository.FindByNormalizedLoginAsync("ANALYST");
        Assert.NotNull(user);
        Assert.Equal(AppUserRole.Analyst, user!.Role);
    }

    [Fact]
    public async Task SignIn_ShouldSetAuthenticatedUser_WhenCredentialsAreValid()
    {
        var store = new InMemorySessionStore();
        var sessionPersistence = new InMemorySessionPersistence();
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);
        var signInHandler = new SignInCommandHandler(store, sessionPersistence, repository, hasher);
        var stateHandler = new GetSessionStateQueryHandler(store);

        var registerResult = await registerHandler.HandleAsync(new RegisterUserCommand("John", "Manager", "manager", "Password1", "Password1"));
        var signInResult = await signInHandler.HandleAsync(new SignInCommand("manager", "Password1"));
        Assert.True(registerResult.IsSuccess);
        Assert.True(signInResult.IsSuccess);
        var stateResult = await stateHandler.HandleAsync(new GetSessionStateQuery());
        Assert.True(stateResult.IsSuccess);
        var state = stateResult.Value;

        Assert.True(state.IsAuthenticated);
        Assert.Equal("manager", state.Login);
        Assert.Equal("John Manager", state.DisplayName);
        Assert.NotNull(sessionPersistence.LastSavedSession);
        Assert.Equal("manager", sessionPersistence.LastSavedSession!.Login);
    }

    [Fact]
    public async Task RestoreSession_ShouldHydrateSession_WhenPersistedUserIsActive()
    {
        var store = new InMemorySessionStore();
        var sessionPersistence = new InMemorySessionPersistence
        {
            SessionToLoad = new PersistedSessionDto(
                PersistedSessionDto.CurrentVersion,
                "manager",
                "John Manager",
                "Manager",
                DateTime.UtcNow)
        };
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);
        var restoreHandler = new RestoreSessionCommandHandler(store, sessionPersistence, repository);

        var registerResult = await registerHandler.HandleAsync(new RegisterUserCommand("John", "Manager", "manager", "Password1", "Password1"));
        Assert.True(registerResult.IsSuccess);
        var restoreResult = await restoreHandler.HandleAsync(new RestoreSessionCommand());
        Assert.True(restoreResult.IsSuccess);

        Assert.True(store.IsAuthenticated);
        Assert.Equal("manager", store.CurrentState.Login);
    }

    [Fact]
    public async Task RestoreSession_ShouldClearPersistedData_WhenUserIsMissing()
    {
        var store = new InMemorySessionStore();
        var sessionPersistence = new InMemorySessionPersistence
        {
            SessionToLoad = new PersistedSessionDto(
                PersistedSessionDto.CurrentVersion,
                "ghost",
                "Ghost User",
                "Analyst",
                DateTime.UtcNow)
        };
        var repository = new InMemoryUserRepository();
        var restoreHandler = new RestoreSessionCommandHandler(store, sessionPersistence, repository);

        var restoreResult = await restoreHandler.HandleAsync(new RestoreSessionCommand());
        Assert.True(restoreResult.IsSuccess);

        Assert.False(store.IsAuthenticated);
        Assert.True(sessionPersistence.ClearCalls > 0);
    }

    [Fact]
    public async Task SignOut_ShouldClearSession()
    {
        var store = new InMemorySessionStore();
        var sessionPersistence = new InMemorySessionPersistence();
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);
        var signInHandler = new SignInCommandHandler(store, sessionPersistence, repository, hasher);
        var signOutHandler = new SignOutCommandHandler(store, sessionPersistence);
        var stateHandler = new GetSessionStateQueryHandler(store);

        var registerResult = await registerHandler.HandleAsync(new RegisterUserCommand("Coach", "Person", "coach", "Password1", "Password1"));
        var signInResult = await signInHandler.HandleAsync(new SignInCommand("coach", "Password1"));
        Assert.True(registerResult.IsSuccess);
        Assert.True(signInResult.IsSuccess);
        await signOutHandler.HandleAsync(new SignOutCommand());

        var stateResult = await stateHandler.HandleAsync(new GetSessionStateQuery());
        Assert.True(stateResult.IsSuccess);
        var state = stateResult.Value;
        Assert.False(state.IsAuthenticated);
        Assert.Null(state.Login);
        Assert.True(sessionPersistence.ClearCalls > 0);
    }

    [Fact]
    public async Task SignOut_ShouldBeIdempotent_WhenSessionIsAlreadyCleared()
    {
        var store = new InMemorySessionStore();
        var sessionPersistence = new InMemorySessionPersistence();
        var signOutHandler = new SignOutCommandHandler(store, sessionPersistence);
        var stateHandler = new GetSessionStateQueryHandler(store);

        await signOutHandler.HandleAsync(new SignOutCommand());
        await signOutHandler.HandleAsync(new SignOutCommand());

        var stateResult = await stateHandler.HandleAsync(new GetSessionStateQuery());
        Assert.True(stateResult.IsSuccess);
        var state = stateResult.Value;
        Assert.False(state.IsAuthenticated);
        Assert.Null(state.Login);
        Assert.True(sessionPersistence.ClearCalls > 0);
    }

    [Fact]
    public async Task SignIn_ShouldFail_WhenPasswordIsInvalid()
    {
        var store = new InMemorySessionStore();
        var sessionPersistence = new InMemorySessionPersistence();
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);
        var signInHandler = new SignInCommandHandler(store, sessionPersistence, repository, hasher);

        var registerResult = await registerHandler.HandleAsync(new RegisterUserCommand("John", "Manager", "manager", "Password1", "Password1"));
        Assert.True(registerResult.IsSuccess);
        var signInResult = await signInHandler.HandleAsync(new SignInCommand("manager", "wrong-password"));
        Assert.True(signInResult.IsFailure);
        Assert.Equal("auth.invalid_credentials", signInResult.Error?.Code);
    }

    [Fact]
    public async Task Register_ShouldFail_WhenLoginAlreadyExists()
    {
        var repository = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var registerHandler = new RegisterUserCommandHandler(repository, hasher);

        var first = await registerHandler.HandleAsync(new RegisterUserCommand("First", "User", "manager", "Password1", "Password1"));
        Assert.True(first.IsSuccess);
        var second = await registerHandler.HandleAsync(new RegisterUserCommand("Second", "User", "manager", "Password1", "Password1"));
        Assert.True(second.IsFailure);
        Assert.Equal("auth.login_exists", second.Error?.Code);
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

    private sealed class InMemorySessionPersistence : ISessionPersistence
    {
        public PersistedSessionDto? SessionToLoad { get; set; }

        public PersistedSessionDto? LastSavedSession { get; private set; }

        public int ClearCalls { get; private set; }

        public Task SaveAsync(PersistedSessionDto session, CancellationToken cancellationToken = default)
        {
            LastSavedSession = session;
            return Task.CompletedTask;
        }

        public Task<PersistedSessionDto?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SessionToLoad);

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            ClearCalls++;
            SessionToLoad = null;
            LastSavedSession = null;
            return Task.CompletedTask;
        }
    }
}
