using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfTeamAttributeRepository(IDbContextFactory<ProFootballDbContext> dbContextFactory) : ITeamAttributeRepository
{
    public async Task AddRangeAsync(IReadOnlyCollection<TeamAttribute> attributes, CancellationToken cancellationToken = default)
    {
        if (attributes.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.TeamAttributes.AddRangeAsync(attributes, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.TeamAttributes.ExecuteDeleteAsync(cancellationToken);
    }
}
