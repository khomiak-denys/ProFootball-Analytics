using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;

namespace ProFootball.Application.Auth.Handlers;

public sealed class SignOutCommandHandler(IUserSessionStore sessionStore) : ICommandHandler<SignOutCommand>
{
    public Task HandleAsync(SignOutCommand command, CancellationToken cancellationToken = default)
    {
        sessionStore.SetCurrentUser(null);
        return Task.CompletedTask;
    }
}
