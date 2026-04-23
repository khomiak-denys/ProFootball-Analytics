using ProFootball.Domain.Entities;

namespace ProFootball.Application.Auth.Abstractions;

public interface IAppUserAuthRepository
{
    Task<AppUser?> FindByNormalizedLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);
}
