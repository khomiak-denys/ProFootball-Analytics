using ProFootball.Application.Auth.Abstractions;

namespace ProFootball.Application.Auth.Handlers;

public sealed class InMemorySessionStore : IUserSessionStore
{
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(CurrentUser);

    public string? CurrentUser { get; private set; }

    public void SetCurrentUser(string? userName)
    {
        CurrentUser = userName;
    }
}
