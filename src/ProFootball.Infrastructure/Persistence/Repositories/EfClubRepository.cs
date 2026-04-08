using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Persistence;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Persistence.Repositories;

public sealed class EfClubRepository(ProFootballDbContext dbContext) : IClubRepository
{
    public async Task<IReadOnlyList<FootballClub>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.FootballClubs
            .AsNoTracking()
            .OrderBy(club => club.Name)
            .ToListAsync(cancellationToken);
    }
}
