using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfPlayerAttributeRepository(ProFootballDbContext dbContext) : IPlayerAttributeRepository
{
    public IQueryable<PlayerAttribute> Query() => dbContext.PlayerAttributes.AsNoTracking();

    public async Task AddRangeAsync(IReadOnlyCollection<PlayerAttribute> attributes, CancellationToken cancellationToken = default)
    {
        if (attributes.Count == 0)
        {
            return;
        }

        await dbContext.PlayerAttributes.AddRangeAsync(attributes, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
        => dbContext.PlayerAttributes.ExecuteDeleteAsync(cancellationToken);
}
