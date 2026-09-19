namespace ProFootball.Domain.Entities;

public sealed class AppUser
{
    private AppUser()
    {
    }

    public AppUser(
        string firstName,
        string lastName,
        string login,
        string passwordHash,
        AppUserRole role,
        bool isActive,
        DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Login = login.Trim().ToLowerInvariant();
        PasswordHash = passwordHash.Trim();
        Role = role;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public int Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Login { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public AppUserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string GetDisplayName() => $"{FirstName} {LastName}";
}
