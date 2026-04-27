using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfFootballMatchRepository(IDbContextFactory<ProFootballDbContext> dbContextFactory) : IFootballMatchRepository
{
    public async Task AddRangeAsync(IReadOnlyCollection<FootballMatch> matches, CancellationToken cancellationToken = default)
    {
        if (matches.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Matches.AddRangeAsync(matches, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Matches.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Matches.CountAsync(cancellationToken);
    }
}
