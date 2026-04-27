namespace ProFootball.Domain.Entities;

public interface ITeamAttributeRepository
{
    Task AddRangeAsync(IReadOnlyCollection<TeamAttribute> attributes, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
