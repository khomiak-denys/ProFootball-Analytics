using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Player.Dtos;
using ProFootball.Application.Player.Queries;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Querying;

public sealed class AnalyticsQueryService(
    IPlayerAttributeRepository playerAttributeRepository,
    IPlayerRepository playerRepository) :
    IQueryHandler<GetPlayerTrendQuery, IReadOnlyList<PlayerTrendPointDto>>,
    IQueryHandler<TopPlayersQuery, IReadOnlyList<TopPlayerDto>>
{
    public async Task<IReadOnlyList<PlayerTrendPointDto>> HandleAsync(
        GetPlayerTrendQuery query,
        CancellationToken cancellationToken = default)
    {
        var playerId = query.PlayerApiId;
        return await playerAttributeRepository.Query()
            .Where(attribute => attribute.PlayerId == playerId)
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
        var limit = query.Limit < 1 ? 20 : query.Limit;
        if (limit > 200)
        {
            limit = 200;
        }

        var attributesQuery = playerAttributeRepository.Query();

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
            var preferredFoot = query.PreferredFoot.Trim().ToLowerInvariant();
            attributesQuery = attributesQuery.Where(attribute =>
                attribute.PreferredFoot != null &&
                attribute.PreferredFoot.ToLower() == preferredFoot);
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
                          join player in playerRepository.Query() on grouped.PlayerId equals player.Id
                          select new TopPlayerDto(
                              grouped.PlayerId,
                              player.Name,
                              Math.Round(grouped.AverageOverallRating, 2),
                              Math.Round(grouped.AveragePotential, 2),
                              grouped.Samples);

        return await queryResult.ToListAsync(cancellationToken);
    }
}
