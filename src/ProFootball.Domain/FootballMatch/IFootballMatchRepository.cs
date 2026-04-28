namespace ProFootball.Domain.Entities;

public interface IFootballMatchRepository
{
    Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);

    Task<FootballMatch?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(FootballMatch match, CancellationToken cancellationToken = default);

    Task UpdateAsync(FootballMatch match, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyCollection<FootballMatch> matches, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
