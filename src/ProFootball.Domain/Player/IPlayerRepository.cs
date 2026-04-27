namespace ProFootball.Domain.Entities;

public interface IPlayerRepository
{
    Task AddRangeAsync(IReadOnlyCollection<Player> players, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
