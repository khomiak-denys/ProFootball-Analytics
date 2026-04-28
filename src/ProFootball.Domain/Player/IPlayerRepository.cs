namespace ProFootball.Domain.Entities;

public interface IPlayerRepository
{
    Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);

    Task<Player?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Player player, CancellationToken cancellationToken = default);

    Task UpdateAsync(Player player, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyCollection<Player> players, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
