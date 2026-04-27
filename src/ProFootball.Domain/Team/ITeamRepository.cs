namespace ProFootball.Domain.Entities;

public interface ITeamRepository
{
    Task AddRangeAsync(IReadOnlyCollection<Team> teams, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
