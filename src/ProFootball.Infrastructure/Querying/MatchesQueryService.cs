using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Application.Team.Dtos;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class MatchesQueryService(IDbContextFactory<ProFootballDbContext> dbContextFactory) :
    IQueryHandler<MatchSearchQuery, PagedResult<MatchListItemDto>>,
    IQueryHandler<GetMatchTeamsQuery, IReadOnlyList<TeamListItemDto>>,
    IQueryHandler<GetMatchDetailsQuery, MatchDetailsDto?>,
    IQueryHandler<GetMatchesBySeasonQuery, IReadOnlyList<MatchesBySeasonDto>>,
    IQueryHandler<GetDashboardKpiQuery, DashboardKpiDto>
{
    public async Task<PagedResult<MatchListItemDto>> HandleAsync(
        MatchSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var (page, pageSize, skip) = Paging.Normalize(query.Page, query.PageSize);

        var projectedQuery = from match in dbContext.Matches.AsNoTracking()
                             join league in dbContext.Leagues.AsNoTracking() on match.LeagueId equals league.Id
                             join homeTeam in dbContext.Teams.AsNoTracking() on match.HomeTeamId equals homeTeam.Id into homeTeamJoin
                             from homeTeam in homeTeamJoin.DefaultIfEmpty()
                             join awayTeam in dbContext.Teams.AsNoTracking() on match.AwayTeamId equals awayTeam.Id into awayTeamJoin
                             from awayTeam in awayTeamJoin.DefaultIfEmpty()
                             select new
                             {
                                 match.Id,
                                 match.Date,
                                 match.Season,
                                 LeagueId = league.Id,
                                 LeagueName = league.Name,
                                 match.CountryName,
                                 match.HomeTeamId,
                                 HomeTeamName = homeTeam == null ? match.HomeTeamId.ToString() : homeTeam.LongName,
                                 match.AwayTeamId,
                                 AwayTeamName = awayTeam == null ? match.AwayTeamId.ToString() : awayTeam.LongName,
                                 match.HomeTeamGoal,
                                 match.AwayTeamGoal,
                             };

        if (query.LeagueId.HasValue)
        {
            projectedQuery = projectedQuery.Where(match => match.LeagueId == query.LeagueId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Season))
        {
            var season = query.Season.Trim();
            projectedQuery = projectedQuery.Where(match => match.Season == season);
        }

        if (query.TeamApiId.HasValue)
        {
            projectedQuery = projectedQuery.Where(match =>
                match.HomeTeamId == query.TeamApiId.Value || match.AwayTeamId == query.TeamApiId.Value);
        }

        if (query.DateFrom.HasValue)
        {
            projectedQuery = projectedQuery.Where(match => match.Date >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            projectedQuery = projectedQuery.Where(match => match.Date <= query.DateTo.Value);
        }

        projectedQuery = (query.SortBy?.Trim().ToLowerInvariant(), query.SortDescending) switch
        {
            ("season", true) => projectedQuery.OrderByDescending(match => match.Season).ThenByDescending(match => match.Date),
            ("season", false) => projectedQuery.OrderBy(match => match.Season).ThenByDescending(match => match.Date),
            ("league", true) => projectedQuery.OrderByDescending(match => match.LeagueName).ThenByDescending(match => match.Date),
            ("league", false) => projectedQuery.OrderBy(match => match.LeagueName).ThenByDescending(match => match.Date),
            (_, true) => projectedQuery.OrderByDescending(match => match.Date),
            _ => projectedQuery.OrderBy(match => match.Date),
        };

        var totalCount = await projectedQuery.CountAsync(cancellationToken);
        var items = await projectedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(match => new MatchListItemDto(
                match.Id,
                match.Date,
                match.Season,
                match.LeagueName,
                match.CountryName,
                match.HomeTeamId,
                match.HomeTeamName,
                match.AwayTeamId,
                match.AwayTeamName,
                match.HomeTeamGoal,
                match.AwayTeamGoal))
            .ToListAsync(cancellationToken);

        return new PagedResult<MatchListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<TeamListItemDto>> HandleAsync(
        GetMatchTeamsQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var matchesQuery = dbContext.Matches.AsNoTracking();
        if (query.LeagueId.HasValue)
        {
            matchesQuery = matchesQuery.Where(match => match.LeagueId == query.LeagueId.Value);
        }

        var teamIds = matchesQuery
            .Select(match => match.HomeTeamId)
            .Concat(matchesQuery.Select(match => match.AwayTeamId))
            .Distinct();

        var teams = await dbContext.Teams
            .AsNoTracking()
            .Where(team => teamIds.Contains(team.Id))
            .OrderBy(team => team.LongName)
            .Select(team => new TeamListItemDto(
                team.Id,
                team.LongName,
                team.ShortName,
                null))
            .ToListAsync(cancellationToken);

        return teams;
    }

    public async Task<MatchDetailsDto?> HandleAsync(
        GetMatchDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var matchId = query.MatchApiId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var match = await (from item in dbContext.Matches.AsNoTracking()
                           where item.Id == matchId
                           join league in dbContext.Leagues.AsNoTracking() on item.LeagueId equals league.Id
                           join homeTeam in dbContext.Teams.AsNoTracking() on item.HomeTeamId equals homeTeam.Id into homeTeamJoin
                           from homeTeam in homeTeamJoin.DefaultIfEmpty()
                           join awayTeam in dbContext.Teams.AsNoTracking() on item.AwayTeamId equals awayTeam.Id into awayTeamJoin
                           from awayTeam in awayTeamJoin.DefaultIfEmpty()
                           select new MatchDetailsDto(
                               item.Id,
                               item.Date,
                               item.Season,
                               league.Name,
                               item.CountryName,
                               item.HomeTeamId,
                               homeTeam == null ? item.HomeTeamId.ToString() : homeTeam.LongName,
                               item.AwayTeamId,
                               awayTeam == null ? item.AwayTeamId.ToString() : awayTeam.LongName,
                               item.HomeTeamGoal,
                               item.AwayTeamGoal))
            .SingleOrDefaultAsync(cancellationToken);

        return match;
    }

    public async Task<IReadOnlyList<MatchesBySeasonDto>> HandleAsync(
        GetMatchesBySeasonQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var matchesQuery = dbContext.Matches.AsNoTracking();

        if (query.LeagueId.HasValue)
        {
            matchesQuery = matchesQuery.Where(match => match.LeagueId == query.LeagueId.Value);
        }

        var groupedQuery = from match in matchesQuery
                           group match by new { match.Season, match.LeagueId }
            into grouped
                           select new
                           {
                               grouped.Key.Season,
                               grouped.Key.LeagueId,
                               MatchCount = grouped.Count(),
                           };

        var queryResult = from grouped in groupedQuery
                          join league in dbContext.Leagues.AsNoTracking() on grouped.LeagueId equals league.Id
                          orderby grouped.Season, league.Name
                          select new MatchesBySeasonDto(
                              grouped.Season,
                              league.Name,
                              grouped.MatchCount);

        return await queryResult.ToListAsync(cancellationToken);
    }

    public async Task<DashboardKpiDto> HandleAsync(
        GetDashboardKpiQuery _,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var countries = await dbContext.Leagues
            .AsNoTracking()
            .Select(league => league.CountryName)
            .Distinct()
            .CountAsync(cancellationToken);
        var leagues = await dbContext.Leagues.CountAsync(cancellationToken);
        var teams = await dbContext.Teams.CountAsync(cancellationToken);
        var players = await dbContext.Players.CountAsync(cancellationToken);
        var matches = await dbContext.Matches.CountAsync(cancellationToken);

        return new DashboardKpiDto(countries, leagues, teams, players, matches);
    }
}
