using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;
using ProFootball.Application.Common;
using ProFootball.Domain.Entities;

namespace ProFootball.Application.Auth.Handlers;

public sealed class SignInCommandHandler(
    IUserSessionStore sessionStore,
    IAppUserAuthRepository userRepository,
    IPasswordHasher passwordHasher) : ICommandHandler<SignInCommand, Result>
{
    public async Task<Result> HandleAsync(SignInCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(command.Login) || string.IsNullOrWhiteSpace(command.Password))
        {
            return Result.Failure("auth.validation", "Login and password are required.");
        }

        var normalizedLogin = command.Login.Trim().ToUpperInvariant();
        var user = await userRepository.FindByNormalizedLoginAsync(normalizedLogin, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure("auth.invalid_credentials", "Invalid login or password.");
        }

        if (!passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            return Result.Failure("auth.invalid_credentials", "Invalid login or password.");
        }

        sessionStore.SetCurrentUser(user.Login, user.GetDisplayName(), user.Role.ToString());
        return Result.Success();
    }
}
