using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfFootballMatchRepository(ProFootballDbContext dbContext) : IFootballMatchRepository
{
    public IQueryable<FootballMatch> Query() => dbContext.Matches.AsNoTracking();

    public async Task AddRangeAsync(IReadOnlyCollection<FootballMatch> matches, CancellationToken cancellationToken = default)
    {
        if (matches.Count == 0)
        {
            return;
        }

        await dbContext.Matches.AddRangeAsync(matches, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
        => dbContext.Matches.ExecuteDeleteAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => dbContext.Matches.CountAsync(cancellationToken);
}
