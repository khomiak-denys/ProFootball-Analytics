namespace ProFootball.Application.Auth.Abstractions;

public interface IUserSessionStore
{
    bool IsAuthenticated { get; }

    string? CurrentUser { get; }

    void SetCurrentUser(string? userName);
}
