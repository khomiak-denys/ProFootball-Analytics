namespace ProFootball.Domain.Entities;

public interface ILeagueRepository
{
    Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);

    Task<League?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(League league, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyCollection<League> leagues, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
