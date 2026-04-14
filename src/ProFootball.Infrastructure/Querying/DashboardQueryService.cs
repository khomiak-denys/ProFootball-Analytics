using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class DashboardQueryService(ProFootballDbContext dbContext) : IDashboardQueryService
{
    public async Task<DashboardKpiDto> GetKpisAsync(CancellationToken cancellationToken = default)
    {
        var countries = await dbContext.Countries.CountAsync(cancellationToken);
        var leagues = await dbContext.Leagues.CountAsync(cancellationToken);
        var teams = await dbContext.Teams.CountAsync(cancellationToken);
        var players = await dbContext.Players.CountAsync(cancellationToken);
        var matches = await dbContext.Matches.CountAsync(cancellationToken);

        return new DashboardKpiDto(countries, leagues, teams, players, matches);
    }
}
