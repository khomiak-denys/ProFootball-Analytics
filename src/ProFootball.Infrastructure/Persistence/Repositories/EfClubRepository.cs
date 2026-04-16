using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Persistence;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfClubRepository(IDbContextFactory<ProFootballDbContext> dbContextFactory) : IClubRepository
{
    public async Task<IReadOnlyList<FootballClub>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.FootballClubs
            .AsNoTracking()
            .OrderBy(club => club.Name)
            .ToListAsync(cancellationToken);
    }
}
