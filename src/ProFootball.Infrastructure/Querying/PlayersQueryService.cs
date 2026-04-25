using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Player.Dtos;
using ProFootball.Application.Player.Queries;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Querying;

public sealed class PlayersQueryService(
    IPlayerRepository playerRepository,
    IPlayerAttributeRepository playerAttributeRepository) :
    IQueryHandler<PlayerSearchQuery, PagedResult<PlayerListItemDto>>,
    IQueryHandler<GetPlayerDetailsQuery, PlayerDetailsDto?>
{
    public async Task<PagedResult<PlayerListItemDto>> HandleAsync(
        PlayerSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, skip) = Paging.Normalize(query.Page, query.PageSize);
        var projectedQuery = BuildProjectedQuery();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLowerInvariant();
            projectedQuery = projectedQuery.Where(player => player.Name.ToLower().Contains(name));
        }

        if (query.MinHeight.HasValue)
        {
            projectedQuery = projectedQuery.Where(player => player.Height.HasValue && player.Height.Value >= query.MinHeight.Value);
        }

        if (query.MaxHeight.HasValue)
        {
            projectedQuery = projectedQuery.Where(player => player.Height.HasValue && player.Height.Value <= query.MaxHeight.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.PreferredFoot))
        {
            var preferredFoot = query.PreferredFoot.Trim().ToLowerInvariant();
            projectedQuery = projectedQuery.Where(player =>
                player.PreferredFoot != null &&
                player.PreferredFoot.ToLower() == preferredFoot);
        }

        if (query.MinOverallRating.HasValue)
        {
            projectedQuery = projectedQuery.Where(player =>
                player.OverallRating.HasValue &&
                player.OverallRating.Value >= query.MinOverallRating.Value);
        }

        if (query.MaxOverallRating.HasValue)
        {
            projectedQuery = projectedQuery.Where(player =>
                player.OverallRating.HasValue &&
                player.OverallRating.Value <= query.MaxOverallRating.Value);
        }

        if (query.MinPotential.HasValue)
        {
            projectedQuery = projectedQuery.Where(player =>
                player.Potential.HasValue &&
                player.Potential.Value >= query.MinPotential.Value);
        }

        if (query.MaxPotential.HasValue)
        {
            projectedQuery = projectedQuery.Where(player =>
                player.Potential.HasValue &&
                player.Potential.Value <= query.MaxPotential.Value);
        }

        projectedQuery = ApplySorting(projectedQuery, query.SortBy, query.SortDescending);

        var totalCount = await projectedQuery.CountAsync(cancellationToken);
        var items = await projectedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(player => new PlayerListItemDto(
                player.Id,
                player.Name,
                player.Birthday,
                player.Height,
                player.Weight,
                player.OverallRating,
                player.Potential,
                player.PreferredFoot))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlayerListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<PlayerDetailsDto?> HandleAsync(
        GetPlayerDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var playerId = query.PlayerApiId;
        var player = await playerRepository.Query()
            .Where(item => item.Id == playerId)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Birthday,
                item.Height,
                item.Weight,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return null;
        }

        var attributes = await playerAttributeRepository.Query()
            .Where(attribute => attribute.PlayerId == playerId)
            .OrderByDescending(attribute => attribute.Date)
            .ThenByDescending(attribute => attribute.Id)
            .Take(300)
            .Select(attribute => new PlayerAttributeDto(
                attribute.Date,
                attribute.OverallRating,
                attribute.Potential,
                attribute.PreferredFoot,
                attribute.AttackingWorkRate,
                attribute.DefensiveWorkRate))
            .ToListAsync(cancellationToken);

        return new PlayerDetailsDto(
            player.Id,
            null,
            player.Name,
            player.Birthday,
            player.Height,
            player.Weight,
            attributes);
    }

    private IQueryable<PlayerProjection> BuildProjectedQuery()
    {
        var playerAttributes = playerAttributeRepository.Query();
        var latestDatesQuery = playerAttributes
            .GroupBy(attribute => attribute.PlayerId)
            .Select(group => new
            {
                PlayerId = group.Key,
                Date = group.Max(attribute => attribute.Date),
            });

        var latestDateRowsQuery =
            from attribute in playerAttributes
            join latestDate in latestDatesQuery
                on new { attribute.PlayerId, attribute.Date } equals new { latestDate.PlayerId, latestDate.Date }
            select attribute;

        var latestIdsQuery = latestDateRowsQuery
            .GroupBy(attribute => attribute.PlayerId)
            .Select(group => new
            {
                PlayerId = group.Key,
                Id = group.Max(attribute => attribute.Id),
            });

        var latestAttributesQuery =
            from attribute in playerAttributes
            join latestId in latestIdsQuery
                on new { attribute.PlayerId, attribute.Id } equals new { latestId.PlayerId, latestId.Id }
            select new
            {
                attribute.PlayerId,
                attribute.OverallRating,
                attribute.Potential,
                attribute.PreferredFoot,
            };

        return
            from player in playerRepository.Query()
            join latest in latestAttributesQuery
                on player.Id equals latest.PlayerId into latestJoin
            from latest in latestJoin.DefaultIfEmpty()
            select new PlayerProjection
            {
                Id = player.Id,
                Name = player.Name,
                Birthday = player.Birthday,
                Height = player.Height,
                Weight = player.Weight,
                OverallRating = latest == null ? null : latest.OverallRating,
                Potential = latest == null ? null : latest.Potential,
                PreferredFoot = latest == null ? null : latest.PreferredFoot,
            };
    }

    private static IQueryable<PlayerProjection> ApplySorting(
        IQueryable<PlayerProjection> projectedQuery,
        string? sortBy,
        bool sortDescending)
    {
        return (sortBy?.Trim().ToLowerInvariant(), sortDescending) switch
        {
            ("overallrating", true) => projectedQuery
                .OrderByDescending(player => player.OverallRating.HasValue)
                .ThenByDescending(player => player.OverallRating)
                .ThenBy(player => player.Name),
            ("overallrating", false) => projectedQuery
                .OrderByDescending(player => player.OverallRating.HasValue)
                .ThenBy(player => player.OverallRating)
                .ThenBy(player => player.Name),
            ("potential", true) => projectedQuery
                .OrderByDescending(player => player.Potential.HasValue)
                .ThenByDescending(player => player.Potential)
                .ThenBy(player => player.Name),
            ("potential", false) => projectedQuery
                .OrderByDescending(player => player.Potential.HasValue)
                .ThenBy(player => player.Potential)
                .ThenBy(player => player.Name),
            ("height", true) => projectedQuery
                .OrderByDescending(player => player.Height.HasValue)
                .ThenByDescending(player => player.Height)
                .ThenBy(player => player.Name),
            ("height", false) => projectedQuery
                .OrderByDescending(player => player.Height.HasValue)
                .ThenBy(player => player.Height)
                .ThenBy(player => player.Name),
            (_, true) => projectedQuery.OrderByDescending(player => player.Name),
            _ => projectedQuery.OrderBy(player => player.Name),
        };
    }

    private sealed class PlayerProjection
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public DateTime? Birthday { get; init; }

        public int? Height { get; init; }

        public int? Weight { get; init; }

        public int? OverallRating { get; init; }

        public int? Potential { get; init; }

        public string? PreferredFoot { get; init; }
    }
}
