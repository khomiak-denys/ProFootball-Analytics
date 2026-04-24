using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfAppUserAuthRepository(IDbContextFactory<ProFootballDbContext> dbContextFactory) : IAppUserAuthRepository
{
    public async Task<AppUser?> FindByNormalizedLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedLogin);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.AppUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.NormalizedLogin == normalizedLogin, cancellationToken);
    }

    public async Task<bool> ExistsByNormalizedLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedLogin);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.AppUsers
            .AnyAsync(user => user.NormalizedLogin == normalizedLogin, cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.AppUsers.CountAsync(cancellationToken);
    }

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.AppUsers.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
