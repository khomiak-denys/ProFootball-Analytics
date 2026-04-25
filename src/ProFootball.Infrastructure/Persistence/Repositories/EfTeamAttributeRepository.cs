using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfTeamAttributeRepository(ProFootballDbContext dbContext) : ITeamAttributeRepository
{
    public IQueryable<TeamAttribute> Query() => dbContext.TeamAttributes.AsNoTracking();

    public async Task AddRangeAsync(IReadOnlyCollection<TeamAttribute> attributes, CancellationToken cancellationToken = default)
    {
        if (attributes.Count == 0)
        {
            return;
        }

        await dbContext.TeamAttributes.AddRangeAsync(attributes, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
        => dbContext.TeamAttributes.ExecuteDeleteAsync(cancellationToken);
}
