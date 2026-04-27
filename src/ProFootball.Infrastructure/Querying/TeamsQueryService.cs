using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Team.Dtos;
using ProFootball.Application.Team.Queries;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class TeamsQueryService(IDbContextFactory<ProFootballDbContext> dbContextFactory) :
    IQueryHandler<TeamSearchQuery, PagedResult<TeamListItemDto>>,
    IQueryHandler<GetTeamDetailsQuery, TeamDetailsDto?>
{
    public async Task<Result<PagedResult<TeamListItemDto>>> HandleAsync(
        TeamSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var (page, pageSize, skip) = Paging.Normalize(query.Page, query.PageSize);

        var teamsQuery = dbContext.Teams.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var pattern = LikePattern.Contains(query.Name.Trim());
            teamsQuery = teamsQuery.Where(team => EF.Functions.ILike(team.LongName, pattern, "\\"));
        }

        teamsQuery = ApplySorting(teamsQuery, query.SortBy, query.SortDescending);

        var totalCount = await teamsQuery.CountAsync(cancellationToken);
        var items = await teamsQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(team => new TeamListItemDto(
                team.Id,
                team.LongName,
                team.ShortName,
                null))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<TeamListItemDto>>.Success(new PagedResult<TeamListItemDto>(items, totalCount, page, pageSize));
    }

    public async Task<Result<TeamDetailsDto?>> HandleAsync(
        GetTeamDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var teamId = query.TeamApiId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var team = await dbContext.Teams
            .AsNoTracking()
            .Where(item => item.Id == teamId)
            .Select(item => new TeamListItemDto(item.Id, item.LongName, item.ShortName, null))
            .SingleOrDefaultAsync(cancellationToken);

        if (team is null)
        {
            return Result<TeamDetailsDto?>.Success(null);
        }

        var attributes = await dbContext.TeamAttributes
            .AsNoTracking()
            .Where(attribute => attribute.TeamId == teamId)
            .OrderByDescending(attribute => attribute.Date)
            .ThenByDescending(attribute => attribute.Id)
            .Take(200)
            .Select(attribute => new TeamAttributeDto(
                attribute.Date,
                attribute.BuildUpPlaySpeed,
                attribute.BuildUpPlayPassing,
                attribute.ChanceCreationPassing,
                attribute.DefencePressure))
            .ToListAsync(cancellationToken);

        return Result<TeamDetailsDto?>.Success(new TeamDetailsDto(team, attributes));
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
            ("teamapiid", true) => query.OrderByDescending(team => team.Id),
            ("teamapiid", false) => query.OrderBy(team => team.Id),
            (_, true) => query.OrderByDescending(team => team.LongName),
            _ => query.OrderBy(team => team.LongName),
        };
    }
}
