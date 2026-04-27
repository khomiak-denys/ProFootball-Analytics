namespace ProFootball.Domain.Entities;

public interface IFootballMatchRepository
{
    Task AddRangeAsync(IReadOnlyCollection<FootballMatch> matches, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
