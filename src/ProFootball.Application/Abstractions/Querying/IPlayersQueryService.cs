using ProFootball.Application.Contracts.Common;
using ProFootball.Application.Contracts.Queries;

namespace ProFootball.Application.Abstractions.Querying;

public interface IPlayersQueryService
{
    Task<PagedResult<PlayerListItemDto>> SearchPlayersAsync(
        PlayerSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<PlayerDetailsDto?> GetPlayerDetailsAsync(
        int playerApiId,
        CancellationToken cancellationToken = default);
}
