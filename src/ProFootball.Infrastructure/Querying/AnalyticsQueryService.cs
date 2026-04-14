using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Analytics;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class AnalyticsQueryService(ProFootballDbContext dbContext) : IAnalyticsQueryService
{
    public async Task<IReadOnlyList<PlayerTrendPointDto>> GetPlayerTrendAsync(
        int playerApiId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlayerAttributes
            .AsNoTracking()
            .Where(attribute => attribute.PlayerApiId == playerApiId)
            .OrderBy(attribute => attribute.Date)
            .Select(attribute => new PlayerTrendPointDto(
                attribute.Date,
                attribute.OverallRating,
                attribute.Potential))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TopPlayerDto>> GetTopPlayersAsync(
        TopPlayersQuery query,
        CancellationToken cancellationToken = default)
    {
        var limit = query.Limit < 1 ? 20 : query.Limit;
        if (limit > 200)
        {
            limit = 200;
        }

        var attributesQuery = dbContext.PlayerAttributes.AsNoTracking();

        if (query.MinOverallRating.HasValue)
        {
            attributesQuery = attributesQuery.Where(attribute =>
                attribute.OverallRating.HasValue && attribute.OverallRating.Value >= query.MinOverallRating.Value);
        }

        if (query.MinPotential.HasValue)
        {
            attributesQuery = attributesQuery.Where(attribute =>
                attribute.Potential.HasValue && attribute.Potential.Value >= query.MinPotential.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.PreferredFoot))
        {
            var foot = query.PreferredFoot.Trim().ToLowerInvariant();
            attributesQuery = attributesQuery.Where(attribute =>
                attribute.PreferredFoot != null &&
                attribute.PreferredFoot.ToLower() == foot);
        }

        var groupedQuery = from attribute in attributesQuery
                           group attribute by attribute.PlayerApiId
            into grouped
                           select new
                           {
                               PlayerApiId = grouped.Key,
                               AverageOverallRating = grouped.Average(attribute => attribute.OverallRating) ?? 0,
                               AveragePotential = grouped.Average(attribute => attribute.Potential) ?? 0,
                               Samples = grouped.Count(),
                           };

        groupedQuery = (query.SortBy?.Trim().ToLowerInvariant(), query.SortDescending) switch
        {
            ("potential", false) => groupedQuery.OrderBy(item => item.AveragePotential),
            ("potential", true) => groupedQuery.OrderByDescending(item => item.AveragePotential),
            ("samples", false) => groupedQuery.OrderBy(item => item.Samples),
            ("samples", true) => groupedQuery.OrderByDescending(item => item.Samples),
            (_, false) => groupedQuery.OrderBy(item => item.AverageOverallRating),
            _ => groupedQuery.OrderByDescending(item => item.AverageOverallRating),
        };

        var topGrouped = groupedQuery.Take(limit);

        var queryResult = from grouped in topGrouped
                          join player in dbContext.Players.AsNoTracking() on grouped.PlayerApiId equals player.PlayerApiId
                          select new TopPlayerDto(
                              grouped.PlayerApiId,
                              player.Name,
                              Math.Round(grouped.AverageOverallRating, 2),
                              Math.Round(grouped.AveragePotential, 2),
                              grouped.Samples);

        return await queryResult.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MatchesBySeasonDto>> GetMatchesBySeasonAsync(
        int? leagueId = null,
        CancellationToken cancellationToken = default)
    {
        var matchesQuery = dbContext.Matches.AsNoTracking();

        if (leagueId.HasValue)
        {
            matchesQuery = matchesQuery.Where(match => match.LeagueId == leagueId.Value);
        }

        var groupedQuery = from match in matchesQuery
                           group match by new { match.Season, match.LeagueId }
            into grouped
                           select new
                           {
                               grouped.Key.Season,
                               grouped.Key.LeagueId,
                               MatchCount = grouped.Count(),
                           };

        var queryResult = from grouped in groupedQuery
                          join league in dbContext.Leagues.AsNoTracking() on grouped.LeagueId equals league.Id
                          orderby grouped.Season, league.Name
                          select new MatchesBySeasonDto(
                              grouped.Season,
                              league.Name,
                              grouped.MatchCount);

        return await queryResult.ToListAsync(cancellationToken);
    }
}
