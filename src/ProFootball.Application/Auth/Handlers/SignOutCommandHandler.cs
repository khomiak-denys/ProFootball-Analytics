using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;

namespace ProFootball.Application.Auth.Handlers;

public sealed class SignOutCommandHandler(IUserSessionStore sessionStore) : ICommandHandler<SignOutCommand>
{
    public Task HandleAsync(SignOutCommand _, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        sessionStore.SetCurrentUser(null);
        return Task.CompletedTask;
    }
}
