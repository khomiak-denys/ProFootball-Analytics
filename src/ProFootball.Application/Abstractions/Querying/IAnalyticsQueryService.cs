using ProFootball.Application.Contracts.Analytics;

namespace ProFootball.Application.Abstractions.Querying;

public interface IAnalyticsQueryService
{
    Task<IReadOnlyList<PlayerTrendPointDto>> GetPlayerTrendAsync(
        int playerApiId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopPlayerDto>> GetTopPlayersAsync(
        TopPlayersQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchesBySeasonDto>> GetMatchesBySeasonAsync(
        int? leagueId = null,
        CancellationToken cancellationToken = default);
}
