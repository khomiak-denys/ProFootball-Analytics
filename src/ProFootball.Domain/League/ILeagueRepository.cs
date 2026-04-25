namespace ProFootball.Domain.Entities;

public interface ILeagueRepository
{
    IQueryable<League> Query();

    Task AddRangeAsync(IReadOnlyCollection<League> leagues, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
