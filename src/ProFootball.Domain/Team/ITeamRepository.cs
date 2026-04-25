namespace ProFootball.Domain.Entities;

public interface ITeamRepository
{
    IQueryable<Team> Query();

    Task AddRangeAsync(IReadOnlyCollection<Team> teams, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
