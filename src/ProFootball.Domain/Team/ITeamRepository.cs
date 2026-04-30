namespace ProFootball.Domain.Entities;

public interface ITeamRepository
{
    Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);

    Task<Team?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Team team, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyCollection<Team> teams, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
