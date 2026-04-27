using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfLeagueRepository(IDbContextFactory<ProFootballDbContext> dbContextFactory) : ILeagueRepository
{
    public async Task AddRangeAsync(IReadOnlyCollection<League> leagues, CancellationToken cancellationToken = default)
    {
        if (leagues.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Leagues.AddRangeAsync(leagues, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Leagues.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Leagues.CountAsync(cancellationToken);
    }
}
