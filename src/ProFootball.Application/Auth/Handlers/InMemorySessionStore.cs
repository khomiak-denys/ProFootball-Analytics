using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Dtos;

namespace ProFootball.Application.Auth.Handlers;

public sealed class InMemorySessionStore : IUserSessionStore
{
    private SessionStateDto _currentState = new(false, null, null, null);

    public bool IsAuthenticated => _currentState.IsAuthenticated;

    public SessionStateDto CurrentState => _currentState;

    public void SetCurrentUser(string login, string displayName, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        _currentState = new SessionStateDto(
            true,
            login.Trim(),
            displayName.Trim(),
            role.Trim());
    }

    public void Clear()
    {
        _currentState = new SessionStateDto(false, null, null, null);
    }
}
