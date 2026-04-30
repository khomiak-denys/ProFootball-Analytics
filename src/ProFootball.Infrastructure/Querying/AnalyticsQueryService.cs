using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Player.Dtos;
using ProFootball.Application.Player.Queries;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class AnalyticsQueryService(IDbContextFactory<ProFootballDbContext> dbContextFactory) :
    IQueryHandler<GetPlayerTrendQuery, IReadOnlyList<PlayerTrendPointDto>>,
    IQueryHandler<TopPlayersQuery, IReadOnlyList<TopPlayerDto>>,
    IQueryHandler<GetTopRatingDeltaPlayersQuery, IReadOnlyList<PlayerRatingDeltaDto>>
{
    public async Task<Result<IReadOnlyList<PlayerTrendPointDto>>> HandleAsync(
        GetPlayerTrendQuery query,
        CancellationToken cancellationToken = default)
    {
        var playerId = query.PlayerApiId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var trend = await dbContext.PlayerAttributes
            .AsNoTracking()
            .Where(attribute => attribute.PlayerId == playerId)
            .OrderBy(attribute => attribute.Date)
            .Select(attribute => new PlayerTrendPointDto(
                attribute.Date,
                attribute.OverallRating,
                attribute.Potential))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PlayerTrendPointDto>>.Success(trend);
    }

    public async Task<Result<IReadOnlyList<TopPlayerDto>>> HandleAsync(
        TopPlayersQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
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
            var footPattern = LikePattern.Exact(query.PreferredFoot.Trim());
            attributesQuery = attributesQuery.Where(attribute =>
                attribute.PreferredFoot != null &&
                EF.Functions.ILike(attribute.PreferredFoot, footPattern, "\\"));
        }

        var groupedQuery = from attribute in attributesQuery
                           group attribute by attribute.PlayerId
            into grouped
                           select new
                           {
                               PlayerId = grouped.Key,
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
                          join player in dbContext.Players.AsNoTracking() on grouped.PlayerId equals player.Id
                          select new TopPlayerDto(
                              grouped.PlayerId,
                              player.Name,
                              Math.Round(grouped.AverageOverallRating, 2),
                              Math.Round(grouped.AveragePotential, 2),
                              grouped.Samples);

        var topPlayers = await queryResult.ToListAsync(cancellationToken);
        return Result<IReadOnlyList<TopPlayerDto>>.Success(topPlayers);
    }

    public async Task<Result<IReadOnlyList<PlayerRatingDeltaDto>>> HandleAsync(
        GetTopRatingDeltaPlayersQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var limit = query.Limit < 1 ? 10 : Math.Min(query.Limit, 50);
        var minimumSamples = query.MinimumSamples < 1 ? 1 : query.MinimumSamples;

        var latestAttributesByPlayer = from attribute in dbContext.PlayerAttributes.AsNoTracking()
                                       where attribute.OverallRating.HasValue && attribute.Potential.HasValue
                                       group attribute by attribute.PlayerId
            into grouped
                                       select grouped
                                           .OrderByDescending(item => item.Date)
                                           .ThenByDescending(item => item.Id)
                                           .Select(item => new
                                           {
                                               item.PlayerId,
                                               item.Date,
                                               Overall = item.OverallRating!.Value,
                                               Potential = item.Potential!.Value,
                                           })
                                           .FirstOrDefault();

        var sampleCounts = from attribute in dbContext.PlayerAttributes.AsNoTracking()
                           group attribute by attribute.PlayerId
            into grouped
                           select new
                           {
                               PlayerId = grouped.Key,
                               Samples = grouped.Count(),
                           };

        var result = await (from latest in latestAttributesByPlayer
                            join player in dbContext.Players.AsNoTracking() on latest.PlayerId equals player.Id
                            join sample in sampleCounts on latest.PlayerId equals sample.PlayerId
                            where sample.Samples >= minimumSamples
                            let delta = latest.Potential - latest.Overall
                            orderby delta descending, latest.Potential descending, player.Name
                            select new PlayerRatingDeltaDto(
                                player.Id,
                                player.Name,
                                latest.Overall,
                                latest.Potential,
                                delta,
                                latest.Date))
            .Take(limit)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PlayerRatingDeltaDto>>.Success(result);
    }
}
