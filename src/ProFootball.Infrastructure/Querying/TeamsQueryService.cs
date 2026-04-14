using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Common;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class TeamsQueryService(ProFootballDbContext dbContext) : ITeamsQueryService
{
    public async Task<PagedResult<TeamListItemDto>> SearchTeamsAsync(
        TeamSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, skip) = Paging.Normalize(query.Page, query.PageSize);

        var teamsQuery = dbContext.Teams.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var pattern = LikePattern.Contains(query.Name.Trim());
            teamsQuery = teamsQuery.Where(team => EF.Functions.ILike(team.LongName, pattern));
        }

        teamsQuery = ApplySorting(teamsQuery, query.SortBy, query.SortDescending);

        var totalCount = await teamsQuery.CountAsync(cancellationToken);
        var items = await teamsQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(team => new TeamListItemDto(
                team.TeamApiId,
                team.LongName,
                team.ShortName,
                team.TeamFifaApiId))
            .ToListAsync(cancellationToken);

        return new PagedResult<TeamListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<TeamDetailsDto?> GetTeamDetailsAsync(
        int teamApiId,
        CancellationToken cancellationToken = default)
    {
        var team = await dbContext.Teams
            .AsNoTracking()
            .Where(item => item.TeamApiId == teamApiId)
            .Select(item => new TeamListItemDto(item.TeamApiId, item.LongName, item.ShortName, item.TeamFifaApiId))
            .SingleOrDefaultAsync(cancellationToken);

        if (team is null)
        {
            return null;
        }

        var attributes = await dbContext.TeamAttributes
            .AsNoTracking()
            .Where(attribute => attribute.TeamApiId == teamApiId)
            .OrderByDescending(attribute => attribute.Date)
            .Take(200)
            .Select(attribute => new TeamAttributeDto(
                attribute.Date,
                attribute.BuildUpPlaySpeed,
                attribute.BuildUpPlayPassing,
                attribute.ChanceCreationPassing,
                attribute.DefencePressure))
            .ToListAsync(cancellationToken);

        return new TeamDetailsDto(team, attributes);
    }

    private static IQueryable<Team> ApplySorting(
        IQueryable<Team> query,
        string? sortBy,
        bool sortDescending)
    {
        return (sortBy?.Trim().ToLowerInvariant(), sortDescending) switch
        {
            ("shortname", true) => query.OrderByDescending(team => team.ShortName).ThenBy(team => team.LongName),
            ("shortname", false) => query.OrderBy(team => team.ShortName).ThenBy(team => team.LongName),
            ("teamapiid", true) => query.OrderByDescending(team => team.TeamApiId),
            ("teamapiid", false) => query.OrderBy(team => team.TeamApiId),
            (_, true) => query.OrderByDescending(team => team.LongName),
            _ => query.OrderBy(team => team.LongName),
        };
    }
}
