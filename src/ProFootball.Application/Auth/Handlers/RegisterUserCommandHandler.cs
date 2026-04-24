using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;
using ProFootball.Domain.Entities;

namespace ProFootball.Application.Auth.Handlers;

public sealed class RegisterUserCommandHandler(
    IAppUserAuthRepository userRepository,
    IPasswordHasher passwordHasher) : ICommandHandler<RegisterUserCommand>
{
    public async Task HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(command.FirstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.LastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Login);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Password);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ConfirmPassword);

        if (!string.Equals(command.Password, command.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new ArgumentException("Password confirmation does not match.", nameof(command.ConfirmPassword));
        }

        if (!MeetsPasswordPolicy(command.Password))
        {
            throw new ArgumentException("Password must be at least 8 characters and contain at least one letter and one digit.", nameof(command.Password));
        }

        var login = command.Login.Trim();
        var normalizedLogin = login.ToUpperInvariant();

        if (await userRepository.ExistsByNormalizedLoginAsync(normalizedLogin, cancellationToken))
        {
            throw new InvalidOperationException("A user with this login already exists.");
        }

        var existingUsers = await userRepository.CountAsync(cancellationToken);
        var role = existingUsers == 0 ? AppUserRole.Admin : AppUserRole.Analyst;
        var passwordHash = passwordHasher.HashPassword(command.Password);

        var user = new AppUser(
            command.FirstName,
            command.LastName,
            login,
            normalizedLogin,
            passwordHash,
            role,
            isActive: true,
            createdAtUtc: DateTime.UtcNow);

        await userRepository.AddAsync(user, cancellationToken);
    }

    private static bool MeetsPasswordPolicy(string password)
    {
        if (password.Length < 8)
        {
            return false;
        }

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        return hasLetter && hasDigit;
    }
}
