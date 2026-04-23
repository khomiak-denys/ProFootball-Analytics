using ProFootball.Application.Auth.Dtos;

namespace ProFootball.Application.Auth.Abstractions;

public interface IUserSessionStore
{
    bool IsAuthenticated { get; }

    SessionStateDto CurrentState { get; }

    void SetCurrentUser(string login, string displayName, string role);

    void Clear();
}
