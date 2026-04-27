using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfTeamRepository(IDbContextFactory<ProFootballDbContext> dbContextFactory) : ITeamRepository
{
    public async Task AddRangeAsync(IReadOnlyCollection<Team> teams, CancellationToken cancellationToken = default)
    {
        if (teams.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Teams.AddRangeAsync(teams, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Teams.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Teams.CountAsync(cancellationToken);
    }
}
