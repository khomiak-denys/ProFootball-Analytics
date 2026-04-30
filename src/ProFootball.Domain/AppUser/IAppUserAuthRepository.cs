namespace ProFootball.Domain.Entities;

public interface IAppUserAuthRepository
{
    Task<AppUser?> FindByLoginAsync(string login, CancellationToken cancellationToken = default);

    Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);
}
