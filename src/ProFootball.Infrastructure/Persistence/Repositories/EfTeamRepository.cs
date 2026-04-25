using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfTeamRepository(ProFootballDbContext dbContext) : ITeamRepository
{
    public IQueryable<Team> Query() => dbContext.Teams.AsNoTracking();

    public async Task AddRangeAsync(IReadOnlyCollection<Team> teams, CancellationToken cancellationToken = default)
    {
        if (teams.Count == 0)
        {
            return;
        }

        await dbContext.Teams.AddRangeAsync(teams, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
        => dbContext.Teams.ExecuteDeleteAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => dbContext.Teams.CountAsync(cancellationToken);
}
