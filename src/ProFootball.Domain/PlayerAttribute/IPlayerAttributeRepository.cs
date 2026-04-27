namespace ProFootball.Domain.Entities;

public interface IPlayerAttributeRepository
{
    Task AddRangeAsync(IReadOnlyCollection<PlayerAttribute> attributes, CancellationToken cancellationToken = default);

    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
