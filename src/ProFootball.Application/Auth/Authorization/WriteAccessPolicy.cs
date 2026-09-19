using ProFootball.Application.Auth.Abstractions;

namespace ProFootball.Application.Auth.Authorization;

internal static class WriteAccessPolicy
{
    public static bool CanManageData(IUserSessionStore sessionStore)
    {
        var role = sessionStore.CurrentState.Role ?? string.Empty;
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
               || role.Equals("Manager", StringComparison.OrdinalIgnoreCase);
    }
}
