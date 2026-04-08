namespace ProFootball.Application.Abstractions.Auth;

public interface ISessionService
{
    bool IsAuthenticated { get; }

    string? CurrentUser { get; }

    void SignIn(string userName);

    void SignOut();
}
