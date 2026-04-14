using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Common;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class PlayersQueryService(ProFootballDbContext dbContext) : IPlayersQueryService
{
    public async Task<PagedResult<PlayerListItemDto>> SearchPlayersAsync(
        PlayerSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, skip) = Paging.Normalize(query.Page, query.PageSize);

        var projectedQuery = dbContext.Players
            .AsNoTracking()
            .Select(player => new
            {
                player.PlayerApiId,
                player.Name,
                player.Height,
                player.Weight,
                LatestAttribute = dbContext.PlayerAttributes
                    .Where(attribute => attribute.PlayerApiId == player.PlayerApiId)
                    .OrderByDescending(attribute => attribute.Date)
                    .Select(attribute => new
                    {
                        attribute.OverallRating,
                        attribute.Potential,
                        attribute.PreferredFoot,
                    })
                    .FirstOrDefault(),
            });

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var normalizedName = query.Name.Trim().ToLowerInvariant();
            projectedQuery = projectedQuery.Where(player => player.Name.ToLower().Contains(normalizedName));
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
            var foot = query.PreferredFoot.Trim().ToLowerInvariant();
            projectedQuery = projectedQuery.Where(player =>
                player.LatestAttribute != null &&
                player.LatestAttribute.PreferredFoot != null &&
                player.LatestAttribute.PreferredFoot.ToLower() == foot);
        }

        if (query.MinOverallRating.HasValue)
        {
            projectedQuery = projectedQuery.Where(player =>
                player.LatestAttribute != null &&
                player.LatestAttribute.OverallRating.HasValue &&
                player.LatestAttribute.OverallRating.Value >= query.MinOverallRating.Value);
        }

        if (query.MaxOverallRating.HasValue)
        {
            projectedQuery = projectedQuery.Where(player =>
                player.LatestAttribute != null &&
                player.LatestAttribute.OverallRating.HasValue &&
                player.LatestAttribute.OverallRating.Value <= query.MaxOverallRating.Value);
        }

        if (query.MinPotential.HasValue)
        {
            projectedQuery = projectedQuery.Where(player =>
                player.LatestAttribute != null &&
                player.LatestAttribute.Potential.HasValue &&
                player.LatestAttribute.Potential.Value >= query.MinPotential.Value);
        }

        if (query.MaxPotential.HasValue)
        {
            projectedQuery = projectedQuery.Where(player =>
                player.LatestAttribute != null &&
                player.LatestAttribute.Potential.HasValue &&
                player.LatestAttribute.Potential.Value <= query.MaxPotential.Value);
        }

        projectedQuery = (query.SortBy?.Trim().ToLowerInvariant(), query.SortDescending) switch
        {
            ("overallrating", true) => projectedQuery.OrderByDescending(player => player.LatestAttribute!.OverallRating).ThenBy(player => player.Name),
            ("overallrating", false) => projectedQuery.OrderBy(player => player.LatestAttribute!.OverallRating).ThenBy(player => player.Name),
            ("potential", true) => projectedQuery.OrderByDescending(player => player.LatestAttribute!.Potential).ThenBy(player => player.Name),
            ("potential", false) => projectedQuery.OrderBy(player => player.LatestAttribute!.Potential).ThenBy(player => player.Name),
            ("height", true) => projectedQuery.OrderByDescending(player => player.Height).ThenBy(player => player.Name),
            ("height", false) => projectedQuery.OrderBy(player => player.Height).ThenBy(player => player.Name),
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
                player.Height,
                player.Weight,
                player.LatestAttribute!.OverallRating,
                player.LatestAttribute!.Potential,
                player.LatestAttribute!.PreferredFoot))
            .ToListAsync(cancellationToken);

        return new PagedResult<PlayerListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<PlayerDetailsDto?> GetPlayerDetailsAsync(
        int playerApiId,
        CancellationToken cancellationToken = default)
    {
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
