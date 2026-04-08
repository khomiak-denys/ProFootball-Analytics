using ProFootball.Application.Abstractions.Auth;

namespace ProFootball.Application.Services.Auth;

public sealed class InMemorySessionService : ISessionService
{
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(CurrentUser);

    public string? CurrentUser { get; private set; }

    public void SignIn(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("Username cannot be empty.", nameof(userName));
        }

        CurrentUser = userName.Trim();
    }

    public void SignOut()
    {
        CurrentUser = null;
    }
}
