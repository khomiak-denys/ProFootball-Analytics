using ProFootball.Application.Contracts.Common;
using ProFootball.Application.Contracts.Queries;

namespace ProFootball.Application.Abstractions.Querying;

public interface IMatchesQueryService
{
    Task<PagedResult<MatchListItemDto>> SearchMatchesAsync(
        MatchSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<MatchDetailsDto?> GetMatchDetailsAsync(
        int matchApiId,
        CancellationToken cancellationToken = default);
}
