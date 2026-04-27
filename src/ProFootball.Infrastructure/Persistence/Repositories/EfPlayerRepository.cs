using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfPlayerRepository(IDbContextFactory<ProFootballDbContext> dbContextFactory) : IPlayerRepository
{
    public async Task AddRangeAsync(IReadOnlyCollection<Player> players, CancellationToken cancellationToken = default)
    {
        if (players.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Players.AddRangeAsync(players, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Players.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Players.CountAsync(cancellationToken);
    }
}
