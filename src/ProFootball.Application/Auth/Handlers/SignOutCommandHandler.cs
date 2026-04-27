using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;

namespace ProFootball.Application.Auth.Handlers;

public sealed class SignOutCommandHandler(
    IUserSessionStore sessionStore,
    ISessionPersistence sessionPersistence) : ICommandHandler<SignOutCommand>
{
    public async Task HandleAsync(SignOutCommand _, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        sessionStore.Clear();
        await sessionPersistence.ClearAsync(cancellationToken);
    }
}
