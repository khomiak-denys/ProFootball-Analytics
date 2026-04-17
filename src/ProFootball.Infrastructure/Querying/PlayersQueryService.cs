using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Common;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class PlayersQueryService(IDbContextFactory<ProFootballDbContext> dbContextFactory) : IPlayersQueryService
{
    public async Task<PagedResult<PlayerListItemDto>> SearchPlayersAsync(
        PlayerSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var (page, pageSize, skip) = Paging.Normalize(query.Page, query.PageSize);

        if (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            return await SearchPlayersSqliteFallbackAsync(dbContext, query, page, pageSize, skip, cancellationToken);
        }

        var latestAttributesQuery = dbContext.PlayerAttributes
            .AsNoTracking()
            .GroupBy(attribute => attribute.PlayerApiId)
            .Select(group => group
                .OrderByDescending(attribute => attribute.Date)
                .ThenByDescending(attribute => attribute.Id)
                .Select(attribute => new
                {
                    attribute.PlayerApiId,
                    attribute.OverallRating,
                    attribute.Potential,
                    attribute.PreferredFoot,
                })
                .First());

        var projectedQuery =
            from player in dbContext.Players.AsNoTracking()
            join latest in latestAttributesQuery
                on player.PlayerApiId equals latest.PlayerApiId into latestJoin
            from latest in latestJoin.DefaultIfEmpty()
            select new
            {
                player.PlayerApiId,
                player.Name,
                player.Birthday,
                player.Height,
                player.Weight,
                OverallRating = latest == null ? null : latest.OverallRating,
                Potential = latest == null ? null : latest.Potential,
                PreferredFoot = latest == null ? null : latest.PreferredFoot,
            };

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var pattern = LikePattern.Contains(query.Name.Trim());
            projectedQuery = projectedQuery.Where(player => EF.Functions.ILike(player.Name, pattern, "\\"));
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
            var footPattern = LikePattern.Exact(query.PreferredFoot.Trim());
            projectedQuery = projectedQuery.Where(player =>
                player.PreferredFoot != null &&
                EF.Functions.ILike(player.PreferredFoot, footPattern, "\\"));
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

        projectedQuery = (query.SortBy?.Trim().ToLowerInvariant(), query.SortDescending) switch
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

        var totalCount = await projectedQuery.CountAsync(cancellationToken);
        var items = await projectedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(player => new PlayerListItemDto(
                player.PlayerApiId,
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

    private static async Task<PagedResult<PlayerListItemDto>> SearchPlayersSqliteFallbackAsync(
        ProFootballDbContext dbContext,
        PlayerSearchQuery query,
        int page,
        int pageSize,
        int skip,
        CancellationToken cancellationToken)
    {
        var players = await dbContext.Players
            .AsNoTracking()
            .Select(player => new PlayerProjection(
                player.PlayerApiId,
                player.Name,
                player.Birthday,
                player.Height,
                player.Weight,
                null,
                null,
                null))
            .ToListAsync(cancellationToken);

        var latestAttributes = await dbContext.PlayerAttributes
            .AsNoTracking()
            .Select(attribute => new PlayerAttributeProjection(
                attribute.Id,
                attribute.PlayerApiId,
                attribute.Date,
                attribute.OverallRating,
                attribute.Potential,
                attribute.PreferredFoot))
            .ToListAsync(cancellationToken);

        var latestByPlayerApiId = latestAttributes
            .GroupBy(attribute => attribute.PlayerApiId)
            .ToDictionary(
                grouped => grouped.Key,
                grouped => grouped
                    .OrderByDescending(attribute => attribute.Date)
                    .ThenByDescending(attribute => attribute.Id)
                    .First());

        IEnumerable<PlayerProjection> projected = players.Select(player =>
        {
            latestByPlayerApiId.TryGetValue(player.PlayerApiId, out var latest);
            return player with
            {
                OverallRating = latest?.OverallRating,
                Potential = latest?.Potential,
                PreferredFoot = latest?.PreferredFoot,
            };
        });

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var nameFilter = query.Name.Trim();
            projected = projected.Where(player =>
                player.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (query.MinHeight.HasValue)
        {
            projected = projected.Where(player => player.Height.HasValue && player.Height.Value >= query.MinHeight.Value);
        }

        if (query.MaxHeight.HasValue)
        {
            projected = projected.Where(player => player.Height.HasValue && player.Height.Value <= query.MaxHeight.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.PreferredFoot))
        {
            var preferredFoot = query.PreferredFoot.Trim();
            projected = projected.Where(player =>
                player.PreferredFoot is not null &&
                string.Equals(player.PreferredFoot, preferredFoot, StringComparison.OrdinalIgnoreCase));
        }

        if (query.MinOverallRating.HasValue)
        {
            projected = projected.Where(player =>
                player.OverallRating.HasValue &&
                player.OverallRating.Value >= query.MinOverallRating.Value);
        }

        if (query.MaxOverallRating.HasValue)
        {
            projected = projected.Where(player =>
                player.OverallRating.HasValue &&
                player.OverallRating.Value <= query.MaxOverallRating.Value);
        }

        if (query.MinPotential.HasValue)
        {
            projected = projected.Where(player =>
                player.Potential.HasValue &&
                player.Potential.Value >= query.MinPotential.Value);
        }

        if (query.MaxPotential.HasValue)
        {
            projected = projected.Where(player =>
                player.Potential.HasValue &&
                player.Potential.Value <= query.MaxPotential.Value);
        }

        projected = (query.SortBy?.Trim().ToLowerInvariant(), query.SortDescending) switch
        {
            ("overallrating", true) => projected
                .OrderByDescending(player => player.OverallRating.HasValue)
                .ThenByDescending(player => player.OverallRating)
                .ThenBy(player => player.Name, StringComparer.Ordinal),
            ("overallrating", false) => projected
                .OrderByDescending(player => player.OverallRating.HasValue)
                .ThenBy(player => player.OverallRating)
                .ThenBy(player => player.Name, StringComparer.Ordinal),
            ("potential", true) => projected
                .OrderByDescending(player => player.Potential.HasValue)
                .ThenByDescending(player => player.Potential)
                .ThenBy(player => player.Name, StringComparer.Ordinal),
            ("potential", false) => projected
                .OrderByDescending(player => player.Potential.HasValue)
                .ThenBy(player => player.Potential)
                .ThenBy(player => player.Name, StringComparer.Ordinal),
            ("height", true) => projected
                .OrderByDescending(player => player.Height.HasValue)
                .ThenByDescending(player => player.Height)
                .ThenBy(player => player.Name, StringComparer.Ordinal),
            ("height", false) => projected
                .OrderByDescending(player => player.Height.HasValue)
                .ThenBy(player => player.Height)
                .ThenBy(player => player.Name, StringComparer.Ordinal),
            (_, true) => projected.OrderByDescending(player => player.Name, StringComparer.Ordinal),
            _ => projected.OrderBy(player => player.Name, StringComparer.Ordinal),
        };

        var materialized = projected.ToList();
        var totalCount = materialized.Count;
        var items = materialized
            .Skip(skip)
            .Take(pageSize)
            .Select(player => new PlayerListItemDto(
                player.PlayerApiId,
                player.Name,
                player.Birthday,
                player.Height,
                player.Weight,
                player.OverallRating,
                player.Potential,
                player.PreferredFoot))
            .ToList();

        return new PagedResult<PlayerListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<PlayerDetailsDto?> GetPlayerDetailsAsync(
        int playerApiId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var player = await dbContext.Players
            .AsNoTracking()
            .Where(item => item.PlayerApiId == playerApiId)
            .Select(item => new
            {
                item.PlayerApiId,
                item.PlayerFifaApiId,
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

        var attributes = await dbContext.PlayerAttributes
            .AsNoTracking()
            .Where(attribute => attribute.PlayerApiId == playerApiId)
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
            player.PlayerApiId,
            player.PlayerFifaApiId,
            player.Name,
            player.Birthday,
            player.Height,
            player.Weight,
            attributes);
    }

    private sealed record PlayerProjection(
        int PlayerApiId,
        string Name,
        DateTime? Birthday,
        int? Height,
        int? Weight,
        int? OverallRating,
        int? Potential,
        string? PreferredFoot);

    private sealed record PlayerAttributeProjection(
        int Id,
        int PlayerApiId,
        DateTime Date,
        int? OverallRating,
        int? Potential,
        string? PreferredFoot);
}
