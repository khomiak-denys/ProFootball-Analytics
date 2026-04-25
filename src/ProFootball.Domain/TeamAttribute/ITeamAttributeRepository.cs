namespace ProFootball.Domain.Entities;

public interface ITeamAttributeRepository
{
    IQueryable<TeamAttribute> Query();

    Task AddRangeAsync(IReadOnlyCollection<TeamAttribute> attributes, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
