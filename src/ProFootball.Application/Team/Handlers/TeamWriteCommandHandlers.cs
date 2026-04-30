using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Authorization;
using ProFootball.Application.Common;
using ProFootball.Application.Team.Commands;
using ProFootball.Domain.Entities;

namespace ProFootball.Application.Team.Handlers;

public sealed class CreateTeamCommandHandler(
    ITeamRepository teamRepository,
    IUserSessionStore sessionStore) : ICommandHandler<CreateTeamCommand, Result>
{
    public async Task<Result> HandleAsync(CreateTeamCommand command, CancellationToken cancellationToken = default)
    {
        if (!WriteAccessPolicy.CanManageData(sessionStore))
        {
            return Result.Failure("auth.forbidden", "You do not have permission to manage teams.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure("team.validation", "Team name is required.");
        }

        var team = new global::ProFootball.Domain.Entities.Team(
            await teamRepository.GetNextIdAsync(cancellationToken),
            command.Name,
            command.ShortName);

        await teamRepository.AddAsync(team, cancellationToken);
        return Result.Success();
    }
}
