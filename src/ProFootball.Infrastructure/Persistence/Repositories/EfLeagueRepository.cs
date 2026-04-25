using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfLeagueRepository(ProFootballDbContext dbContext) : ILeagueRepository
{
    public IQueryable<League> Query() => dbContext.Leagues.AsNoTracking();

    public async Task AddRangeAsync(IReadOnlyCollection<League> leagues, CancellationToken cancellationToken = default)
    {
        if (leagues.Count == 0)
        {
            return;
        }

        await dbContext.Leagues.AddRangeAsync(leagues, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
        => dbContext.Leagues.ExecuteDeleteAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => dbContext.Leagues.CountAsync(cancellationToken);
}
