using ProFootball.Domain.Entities;

namespace ProFootball.Application.Abstractions.Persistence;

public interface IClubRepository
{
    Task<IReadOnlyList<FootballClub>> GetAllAsync(CancellationToken cancellationToken = default);
}
