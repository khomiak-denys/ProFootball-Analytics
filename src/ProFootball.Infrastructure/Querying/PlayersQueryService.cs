using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Player.Dtos;
using ProFootball.Application.Player.Queries;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class PlayersQueryService(IDbContextFactory<ProFootballDbContext> dbContextFactory) :
    IQueryHandler<PlayerSearchQuery, PagedResult<PlayerListItemDto>>,
    IQueryHandler<GetPlayerDetailsQuery, PlayerDetailsDto?>
{
    private const string SqliteProviderName = "Microsoft.EntityFrameworkCore.Sqlite";

    public async Task<PagedResult<PlayerListItemDto>> HandleAsync(
        PlayerSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var (page, pageSize, skip) = Paging.Normalize(query.Page, query.PageSize);

        return string.Equals(dbContext.Database.ProviderName, SqliteProviderName, StringComparison.Ordinal)
            ? await SearchPlayersSqliteAsync(dbContext, query, page, pageSize, skip, cancellationToken)
            : await SearchPlayersPostgresAsync(dbContext, query, page, pageSize, skip, cancellationToken);
    }

    private static async Task<PagedResult<PlayerListItemDto>> SearchPlayersPostgresAsync(
        ProFootballDbContext dbContext,
        PlayerSearchQuery query,
        int page,
        int pageSize,
        int skip,
        CancellationToken cancellationToken)
    {
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

    private static async Task<PagedResult<PlayerListItemDto>> SearchPlayersSqliteAsync(
        ProFootballDbContext dbContext,
        PlayerSearchQuery query,
        int page,
        int pageSize,
        int skip,
        CancellationToken cancellationToken)
    {
        var playerAttributes = dbContext.PlayerAttributes.AsNoTracking();
        var latestDatesQuery = playerAttributes
            .GroupBy(attribute => attribute.PlayerApiId)
            .Select(group => new
            {
                PlayerApiId = group.Key,
                Date = group.Max(attribute => attribute.Date),
            });

        var latestDateRowsQuery =
            from attribute in playerAttributes
            join latestDate in latestDatesQuery
                on new { attribute.PlayerApiId, attribute.Date } equals new { latestDate.PlayerApiId, latestDate.Date }
            select attribute;

        var latestIdsQuery = latestDateRowsQuery
            .GroupBy(attribute => attribute.PlayerApiId)
            .Select(group => new
            {
                PlayerApiId = group.Key,
                Id = group.Max(attribute => attribute.Id),
            });

        var latestAttributesQuery =
            from attribute in playerAttributes
            join latestId in latestIdsQuery
                on new { attribute.PlayerApiId, attribute.Id } equals new { latestId.PlayerApiId, latestId.Id }
            select new
            {
                attribute.PlayerApiId,
                attribute.OverallRating,
                attribute.Potential,
                attribute.PreferredFoot,
            };

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
            projectedQuery = projectedQuery.Where(player =>
                EF.Functions.Like(EF.Functions.Collate(player.Name, "NOCASE"), pattern, "\\"));
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
                EF.Functions.Like(EF.Functions.Collate(player.PreferredFoot, "NOCASE"), footPattern, "\\"));
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

    public async Task<PlayerDetailsDto?> HandleAsync(
        GetPlayerDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var playerApiId = query.PlayerApiId;
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
}
