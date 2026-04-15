using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class DashboardQueryService(IDbContextFactory<ProFootballDbContext> dbContextFactory) : IDashboardQueryService
{
    public async Task<DashboardKpiDto> GetKpisAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var countries = await dbContext.Countries.CountAsync(cancellationToken);
        var leagues = await dbContext.Leagues.CountAsync(cancellationToken);
        var teams = await dbContext.Teams.CountAsync(cancellationToken);
        var players = await dbContext.Players.CountAsync(cancellationToken);
        var matches = await dbContext.Matches.CountAsync(cancellationToken);

        return new DashboardKpiDto(countries, leagues, teams, players, matches);
    }
}
