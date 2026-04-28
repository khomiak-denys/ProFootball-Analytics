using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Common;
using ProFootball.Application.Match.Commands;
using ProFootball.Domain.Entities;

namespace ProFootball.Application.Match.Handlers;

public sealed class CreateMatchCommandHandler(
    IFootballMatchRepository matchRepository,
    IUserSessionStore sessionStore) : ICommandHandler<CreateMatchCommand, Result>
{
    public async Task<Result> HandleAsync(CreateMatchCommand command, CancellationToken cancellationToken = default)
    {
        if (!CanManage(sessionStore))
        {
            return Result.Failure("auth.forbidden", "You do not have permission to manage matches.");
        }

        if (!IsValid(command.CountryName, command.Season, command.LeagueId, command.HomeTeamId, command.AwayTeamId))
        {
            return Result.Failure("match.validation", "Invalid match data.");
        }

        var match = new FootballMatch(
            await matchRepository.GetNextIdAsync(cancellationToken),
            command.CountryName,
            command.LeagueId,
            command.Season,
            command.Date,
            command.HomeTeamId,
            command.AwayTeamId,
            command.HomeTeamGoal,
            command.AwayTeamGoal);

        await matchRepository.AddAsync(match, cancellationToken);
        return Result.Success();
    }

    private static bool IsValid(string country, string season, int leagueId, int homeTeamId, int awayTeamId) =>
        !string.IsNullOrWhiteSpace(country)
        && !string.IsNullOrWhiteSpace(season)
        && leagueId > 0
        && homeTeamId > 0
        && awayTeamId > 0;

    private static bool CanManage(IUserSessionStore sessionStore)
    {
        var role = sessionStore.CurrentState.Role ?? string.Empty;
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
               || role.Equals("Manager", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class UpdateMatchCommandHandler(
    IFootballMatchRepository matchRepository,
    IUserSessionStore sessionStore) : ICommandHandler<UpdateMatchCommand, Result>
{
    public async Task<Result> HandleAsync(UpdateMatchCommand command, CancellationToken cancellationToken = default)
    {
        if (!CanManage(sessionStore))
        {
            return Result.Failure("auth.forbidden", "You do not have permission to manage matches.");
        }

        if (!IsValid(command.CountryName, command.Season, command.LeagueId, command.HomeTeamId, command.AwayTeamId))
        {
            return Result.Failure("match.validation", "Invalid match data.");
        }

        var match = await matchRepository.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return Result.Failure("match.not_found", "Match was not found.");
        }

        match.Update(
            command.CountryName,
            command.LeagueId,
            command.Season,
            command.Date,
            command.HomeTeamId,
            command.AwayTeamId,
            command.HomeTeamGoal,
            command.AwayTeamGoal);

        await matchRepository.UpdateAsync(match, cancellationToken);
        return Result.Success();
    }

    private static bool IsValid(string country, string season, int leagueId, int homeTeamId, int awayTeamId) =>
        !string.IsNullOrWhiteSpace(country)
        && !string.IsNullOrWhiteSpace(season)
        && leagueId > 0
        && homeTeamId > 0
        && awayTeamId > 0;

    private static bool CanManage(IUserSessionStore sessionStore)
    {
        var role = sessionStore.CurrentState.Role ?? string.Empty;
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
               || role.Equals("Manager", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class DeleteMatchCommandHandler(
    IFootballMatchRepository matchRepository,
    IUserSessionStore sessionStore) : ICommandHandler<DeleteMatchCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteMatchCommand command, CancellationToken cancellationToken = default)
    {
        if (!CanManage(sessionStore))
        {
            return Result.Failure("auth.forbidden", "You do not have permission to manage matches.");
        }

        var match = await matchRepository.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return Result.Failure("match.not_found", "Match was not found.");
        }

        await matchRepository.DeleteAsync(command.MatchId, cancellationToken);
        return Result.Success();
    }

    private static bool CanManage(IUserSessionStore sessionStore)
    {
        var role = sessionStore.CurrentState.Role ?? string.Empty;
        return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
               || role.Equals("Manager", StringComparison.OrdinalIgnoreCase);
    }
}
