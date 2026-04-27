using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfPlayerAttributeRepository(IDbContextFactory<ProFootballDbContext> dbContextFactory) : IPlayerAttributeRepository
{
    public async Task AddRangeAsync(IReadOnlyCollection<PlayerAttribute> attributes, CancellationToken cancellationToken = default)
    {
        if (attributes.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.PlayerAttributes.AddRangeAsync(attributes, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.PlayerAttributes.ExecuteDeleteAsync(cancellationToken);
    }
}
