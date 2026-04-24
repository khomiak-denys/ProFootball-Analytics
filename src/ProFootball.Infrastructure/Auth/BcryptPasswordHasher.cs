using ProFootball.Application.Auth.Abstractions;

namespace ProFootball.Infrastructure.Auth;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
