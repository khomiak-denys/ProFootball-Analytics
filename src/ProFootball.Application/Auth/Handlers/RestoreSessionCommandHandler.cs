using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;
using ProFootball.Application.Common;
using ProFootball.Domain.Entities;

namespace ProFootball.Application.Auth.Handlers;

public sealed class RestoreSessionCommandHandler(
    IUserSessionStore sessionStore,
    ISessionPersistence sessionPersistence,
    IAppUserAuthRepository userRepository) : ICommandHandler<RestoreSessionCommand, Result>
{
    public async Task<Result> HandleAsync(RestoreSessionCommand _, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var persisted = await sessionPersistence.LoadAsync(cancellationToken);
        if (persisted is null)
        {
            sessionStore.Clear();
            return Result.Success();
        }

        if (string.IsNullOrWhiteSpace(persisted.Login))
        {
            await sessionPersistence.ClearAsync(cancellationToken);
            sessionStore.Clear();
            return Result.Success();
        }

        var login = persisted.Login.Trim().ToLowerInvariant();
        var user = await userRepository.FindByLoginAsync(login, cancellationToken);
        if (user is null || !user.IsActive)
        {
            await sessionPersistence.ClearAsync(cancellationToken);
            sessionStore.Clear();
            return Result.Success();
        }

        sessionStore.SetCurrentUser(user.Login, user.GetDisplayName(), user.Role.ToString());
        return Result.Success();
    }
}
