using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Team.Dtos;
using ProFootball.Application.Team.Queries;
using ProFootball.Domain.Entities;

namespace ProFootball.Infrastructure.Querying;

public sealed class TeamsQueryService(
    ITeamRepository teamRepository,
    ITeamAttributeRepository teamAttributeRepository) :
    IQueryHandler<TeamSearchQuery, PagedResult<TeamListItemDto>>,
    IQueryHandler<GetTeamDetailsQuery, TeamDetailsDto?>
{
    public async Task<PagedResult<TeamListItemDto>> HandleAsync(
        TeamSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize, skip) = Paging.Normalize(query.Page, query.PageSize);

        var teamsQuery = teamRepository.Query();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim().ToLowerInvariant();
            teamsQuery = teamsQuery.Where(team => team.LongName.ToLower().Contains(name));
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

        return new PagedResult<TeamListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<TeamDetailsDto?> HandleAsync(
        GetTeamDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var teamId = query.TeamApiId;
        var team = await teamRepository.Query()
            .Where(item => item.Id == teamId)
            .Select(item => new TeamListItemDto(item.Id, item.LongName, item.ShortName, null))
            .SingleOrDefaultAsync(cancellationToken);

        if (team is null)
        {
            return null;
        }

        var attributes = await teamAttributeRepository.Query()
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
            ("teamapiid", true) => query.OrderByDescending(team => team.Id),
            ("teamapiid", false) => query.OrderBy(team => team.Id),
            (_, true) => query.OrderByDescending(team => team.LongName),
            _ => query.OrderBy(team => team.LongName),
        };
    }
}
