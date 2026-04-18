using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Player.Dtos;
using ProFootball.Application.Player.Queries;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class AnalyticsQueryService(IDbContextFactory<ProFootballDbContext> dbContextFactory) :
    IQueryHandler<GetPlayerTrendQuery, IReadOnlyList<PlayerTrendPointDto>>,
    IQueryHandler<TopPlayersQuery, IReadOnlyList<TopPlayerDto>>
{
    public async Task<IReadOnlyList<PlayerTrendPointDto>> HandleAsync(
        GetPlayerTrendQuery query,
        CancellationToken cancellationToken = default)
    {
        var playerApiId = query.PlayerApiId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
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

    public async Task<IReadOnlyList<TopPlayerDto>> HandleAsync(
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
}
