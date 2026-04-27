using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;
using ProFootball.Application.Common;
using ProFootball.Domain.Entities;

namespace ProFootball.Application.Auth.Handlers;

public sealed class RegisterUserCommandHandler(
    IAppUserAuthRepository userRepository,
    IPasswordHasher passwordHasher) : ICommandHandler<RegisterUserCommand, Result>
{
    public async Task<Result> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(command.FirstName)
            || string.IsNullOrWhiteSpace(command.LastName)
            || string.IsNullOrWhiteSpace(command.Login)
            || string.IsNullOrWhiteSpace(command.Password)
            || string.IsNullOrWhiteSpace(command.ConfirmPassword))
        {
            return Result.Failure("auth.validation", "First name, last name, login and passwords are required.");
        }

        if (!string.Equals(command.Password, command.ConfirmPassword, StringComparison.Ordinal))
        {
            return Result.Failure("auth.password_mismatch", "Password confirmation does not match.");
        }

        if (!MeetsPasswordPolicy(command.Password))
        {
            return Result.Failure("auth.password_policy", "Password must be at least 8 characters and contain at least one letter and one digit.");
        }

        var login = command.Login.Trim();
        var normalizedLogin = login.ToUpperInvariant();

        if (await userRepository.ExistsByNormalizedLoginAsync(normalizedLogin, cancellationToken))
        {
            return Result.Failure("auth.login_exists", "A user with this login already exists.");
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
        return Result.Success();
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
