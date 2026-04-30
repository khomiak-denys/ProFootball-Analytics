using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfFootballMatchRepository(IDbContextFactory<ProFootballDbContext> dbContextFactory) : IFootballMatchRepository
{
    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var maxId = await dbContext.Matches.MaxAsync(match => (int?)match.Id, cancellationToken) ?? 0;
        return maxId + 1;
    }

    public async Task<FootballMatch?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Matches.FirstOrDefaultAsync(match => match.Id == id, cancellationToken);
    }

    public async Task AddAsync(FootballMatch match, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Matches.AddAsync(match, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FootballMatch match, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Matches.Update(match);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Matches.Where(match => match.Id == id).ExecuteDeleteAsync(cancellationToken);
    }

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
