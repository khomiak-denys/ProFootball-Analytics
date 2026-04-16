using ProFootball.Application.Contracts.Common;
using ProFootball.Application.Contracts.Queries;

namespace ProFootball.Application.Abstractions.Querying;

public interface ITeamsQueryService
{
    Task<PagedResult<TeamListItemDto>> SearchTeamsAsync(
        TeamSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<TeamDetailsDto?> GetTeamDetailsAsync(
        int teamApiId,
        CancellationToken cancellationToken = default);
}
