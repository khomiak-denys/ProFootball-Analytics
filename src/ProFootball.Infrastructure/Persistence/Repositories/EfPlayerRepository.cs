using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfPlayerRepository(ProFootballDbContext dbContext) : IPlayerRepository
{
    public IQueryable<Player> Query() => dbContext.Players.AsNoTracking();

    public async Task AddRangeAsync(IReadOnlyCollection<Player> players, CancellationToken cancellationToken = default)
    {
        if (players.Count == 0)
        {
            return;
        }

        await dbContext.Players.AddRangeAsync(players, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
        => dbContext.Players.ExecuteDeleteAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => dbContext.Players.CountAsync(cancellationToken);
}
