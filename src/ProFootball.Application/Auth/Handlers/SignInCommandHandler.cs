using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;

namespace ProFootball.Application.Auth.Handlers;

public sealed class SignInCommandHandler(IUserSessionStore sessionStore) : ICommandHandler<SignInCommand>
{
    public Task HandleAsync(SignInCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.UserName);
        sessionStore.SetCurrentUser(command.UserName.Trim());
        return Task.CompletedTask;
    }
}
