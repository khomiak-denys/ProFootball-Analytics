using ProFootball.Application.Auth.Dtos;

namespace ProFootball.Application.Auth.Abstractions;

public interface ISessionPersistence
{
    Task SaveAsync(PersistedSessionDto session, CancellationToken cancellationToken = default);

    Task<PersistedSessionDto?> LoadAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
