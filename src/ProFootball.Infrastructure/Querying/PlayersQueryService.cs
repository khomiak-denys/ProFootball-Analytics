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

    public async Task<Result<PagedResult<PlayerListItemDto>>> HandleAsync(
        PlayerSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var (page, pageSize, skip) = Paging.Normalize(query.Page, query.PageSize);

        var pagedResult = string.Equals(dbContext.Database.ProviderName, SqliteProviderName, StringComparison.Ordinal)
            ? await SearchPlayersSqliteAsync(dbContext, query, page, pageSize, skip, cancellationToken)
            : await SearchPlayersPostgresAsync(dbContext, query, page, pageSize, skip, cancellationToken);

        return Result<PagedResult<PlayerListItemDto>>.Success(pagedResult);
    }

    private static async Task<PagedResult<PlayerListItemDto>> SearchPlayersPostgresAsync(
        ProFootballDbContext dbContext,
        PlayerSearchQuery query,
        int page,
        int pageSize,
        int skip,
        CancellationToken cancellationToken)
    {
        var playerAttributes = dbContext.PlayerAttributes.AsNoTracking();
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

        var projectedQuery =
            from player in dbContext.Players.AsNoTracking()
            join latest in latestAttributesQuery
                on player.Id equals latest.PlayerId into latestJoin
            from latest in latestJoin.DefaultIfEmpty()
            select new
            {
                player.Id,
                player.FirstName,
                player.LastName,
                FullName = (player.FirstName + " " + player.LastName).Trim(),
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
            projectedQuery = projectedQuery.Where(player => EF.Functions.ILike(player.FullName, pattern, "\\"));
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
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            ("overallrating", false) => projectedQuery
                .OrderByDescending(player => player.OverallRating.HasValue)
                .ThenBy(player => player.OverallRating)
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            ("potential", true) => projectedQuery
                .OrderByDescending(player => player.Potential.HasValue)
                .ThenByDescending(player => player.Potential)
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            ("potential", false) => projectedQuery
                .OrderByDescending(player => player.Potential.HasValue)
                .ThenBy(player => player.Potential)
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            ("height", true) => projectedQuery
                .OrderByDescending(player => player.Height.HasValue)
                .ThenByDescending(player => player.Height)
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            ("height", false) => projectedQuery
                .OrderByDescending(player => player.Height.HasValue)
                .ThenBy(player => player.Height)
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            (_, true) => projectedQuery.OrderByDescending(player => player.LastName).ThenByDescending(player => player.FirstName),
            _ => projectedQuery.OrderBy(player => player.LastName).ThenBy(player => player.FirstName),
        };

        var totalCount = await projectedQuery.CountAsync(cancellationToken);
        var items = await projectedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(player => new PlayerListItemDto(
                player.Id,
                player.FirstName,
                player.LastName,
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

        var projectedQuery =
            from player in dbContext.Players.AsNoTracking()
            join latest in latestAttributesQuery
                on player.Id equals latest.PlayerId into latestJoin
            from latest in latestJoin.DefaultIfEmpty()
            select new
            {
                player.Id,
                player.FirstName,
                player.LastName,
                FullName = (player.FirstName + " " + player.LastName).Trim(),
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
                EF.Functions.Like(EF.Functions.Collate(player.FullName, "NOCASE"), pattern, "\\"));
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
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            ("overallrating", false) => projectedQuery
                .OrderByDescending(player => player.OverallRating.HasValue)
                .ThenBy(player => player.OverallRating)
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            ("potential", true) => projectedQuery
                .OrderByDescending(player => player.Potential.HasValue)
                .ThenByDescending(player => player.Potential)
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            ("potential", false) => projectedQuery
                .OrderByDescending(player => player.Potential.HasValue)
                .ThenBy(player => player.Potential)
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            ("height", true) => projectedQuery
                .OrderByDescending(player => player.Height.HasValue)
                .ThenByDescending(player => player.Height)
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            ("height", false) => projectedQuery
                .OrderByDescending(player => player.Height.HasValue)
                .ThenBy(player => player.Height)
                .ThenBy(player => player.LastName)
                .ThenBy(player => player.FirstName),
            (_, true) => projectedQuery.OrderByDescending(player => player.LastName).ThenByDescending(player => player.FirstName),
            _ => projectedQuery.OrderBy(player => player.LastName).ThenBy(player => player.FirstName),
        };

        var totalCount = await projectedQuery.CountAsync(cancellationToken);
        var items = await projectedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(player => new PlayerListItemDto(
                player.Id,
                player.FirstName,
                player.LastName,
                player.Birthday,
                player.Height,
                player.Weight,
                player.OverallRating,
                player.Potential,
                player.PreferredFoot))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlayerListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<Result<PlayerDetailsDto?>> HandleAsync(
        GetPlayerDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var playerId = query.PlayerApiId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var player = await dbContext.Players
            .AsNoTracking()
            .Where(item => item.Id == playerId)
            .Select(item => new
            {
                item.Id,
                item.FirstName,
                item.LastName,
                item.Birthday,
                item.Height,
                item.Weight,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return Result<PlayerDetailsDto?>.Success(null);
        }

        var attributes = await dbContext.PlayerAttributes
            .AsNoTracking()
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

        return Result<PlayerDetailsDto?>.Success(new PlayerDetailsDto(
            player.Id,
            null,
            player.FirstName,
            player.LastName,
            player.Birthday,
            player.Height,
            player.Weight,
            attributes));
    }
}
