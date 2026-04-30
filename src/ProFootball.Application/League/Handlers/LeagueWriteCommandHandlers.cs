using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Authorization;
using ProFootball.Application.Common;
using ProFootball.Application.League.Commands;
using ProFootball.Domain.Entities;

namespace ProFootball.Application.League.Handlers;

public sealed class CreateLeagueCommandHandler(
    ILeagueRepository leagueRepository,
    IUserSessionStore sessionStore) : ICommandHandler<CreateLeagueCommand, Result>
{
    public async Task<Result> HandleAsync(CreateLeagueCommand command, CancellationToken cancellationToken = default)
    {
        if (!WriteAccessPolicy.CanManageData(sessionStore))
        {
            return Result.Failure("auth.forbidden", "You do not have permission to manage leagues.");
        }

        if (string.IsNullOrWhiteSpace(command.CountryName) || string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure("league.validation", "League name and country are required.");
        }

        if (command.MaxTeams is <= 0)
        {
            return Result.Failure("league.validation", "Maximum teams must be greater than 0.");
        }

        var league = new global::ProFootball.Domain.Entities.League(
            await leagueRepository.GetNextIdAsync(cancellationToken),
            command.CountryName,
            command.Name,
            command.MaxTeams,
            command.Description);

        await leagueRepository.AddAsync(league, cancellationToken);
        return Result.Success();
    }
}
