using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Authorization;
using ProFootball.Application.Common;
using ProFootball.Application.Player.Commands;
using ProFootball.Domain.Entities;

namespace ProFootball.Application.Player.Handlers;

public sealed class CreatePlayerCommandHandler(
    IPlayerRepository playerRepository,
    IUserSessionStore sessionStore) : ICommandHandler<CreatePlayerCommand, Result>
{
    public async Task<Result> HandleAsync(CreatePlayerCommand command, CancellationToken cancellationToken = default)
    {
        if (!WriteAccessPolicy.CanManageData(sessionStore))
        {
            return Result.Failure("auth.forbidden", "You do not have permission to manage players.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure("player.validation", "Player name is required.");
        }

        var player = new global::ProFootball.Domain.Entities.Player(
            await playerRepository.GetNextIdAsync(cancellationToken),
            command.Name,
            command.Birthday,
            command.Height,
            command.Weight);

        await playerRepository.AddAsync(player, cancellationToken);
        return Result.Success();
    }
}

public sealed class UpdatePlayerCommandHandler(
    IPlayerRepository playerRepository,
    IUserSessionStore sessionStore) : ICommandHandler<UpdatePlayerCommand, Result>
{
    public async Task<Result> HandleAsync(UpdatePlayerCommand command, CancellationToken cancellationToken = default)
    {
        if (!WriteAccessPolicy.CanManageData(sessionStore))
        {
            return Result.Failure("auth.forbidden", "You do not have permission to manage players.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure("player.validation", "Player name is required.");
        }

        var player = await playerRepository.GetByIdAsync(command.PlayerId, cancellationToken);
        if (player is null)
        {
            return Result.Failure("player.not_found", "Player was not found.");
        }

        player.UpdateProfile(command.Name, command.Birthday, command.Height, command.Weight);
        await playerRepository.UpdateAsync(player, cancellationToken);
        return Result.Success();
    }
}

public sealed class DeletePlayerCommandHandler(
    IPlayerRepository playerRepository,
    IUserSessionStore sessionStore) : ICommandHandler<DeletePlayerCommand, Result>
{
    public async Task<Result> HandleAsync(DeletePlayerCommand command, CancellationToken cancellationToken = default)
    {
        if (!WriteAccessPolicy.CanManageData(sessionStore))
        {
            return Result.Failure("auth.forbidden", "You do not have permission to manage players.");
        }

        var player = await playerRepository.GetByIdAsync(command.PlayerId, cancellationToken);
        if (player is null)
        {
            return Result.Failure("player.not_found", "Player was not found.");
        }

        await playerRepository.DeleteAsync(command.PlayerId, cancellationToken);
        return Result.Success();
    }
}
