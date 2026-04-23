using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;

namespace ProFootball.Application.Auth.Handlers;

public sealed class SignInCommandHandler(
    IUserSessionStore sessionStore,
    IAppUserAuthRepository userRepository,
    IPasswordHasher passwordHasher) : ICommandHandler<SignInCommand>
{
    public async Task HandleAsync(SignInCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(command.Login);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Password);

        var normalizedLogin = command.Login.Trim().ToUpperInvariant();
        var user = await userRepository.FindByNormalizedLoginAsync(normalizedLogin, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid login or password.");
        }

        if (!passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid login or password.");
        }

        sessionStore.SetCurrentUser(user.Login, user.GetDisplayName(), user.Role.ToString());
    }
}
