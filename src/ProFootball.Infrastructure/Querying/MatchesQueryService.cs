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
    IQueryHandler<GetDashboardKpiQuery, DashboardKpiDto>,
    IQueryHandler<GetSeasonRatingTrendQuery, IReadOnlyList<SeasonRatingTrendPointDto>>,
    IQueryHandler<GetMatchOutcomeDistributionQuery, MatchOutcomeDistributionDto>,
    IQueryHandler<GetLeagueCompetitivenessQuery, IReadOnlyList<LeagueCompetitivenessDto>>
{
    public async Task<Result<PagedResult<MatchListItemDto>>> HandleAsync(
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

        return Result<PagedResult<MatchListItemDto>>.Success(new PagedResult<MatchListItemDto>(items, totalCount, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<TeamListItemDto>>> HandleAsync(
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

        return Result<IReadOnlyList<TeamListItemDto>>.Success(teams);
    }

    public async Task<Result<MatchDetailsDto?>> HandleAsync(
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

        return Result<MatchDetailsDto?>.Success(match);
    }

    public async Task<Result<IReadOnlyList<MatchesBySeasonDto>>> HandleAsync(
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

        var seasons = await queryResult.ToListAsync(cancellationToken);
        return Result<IReadOnlyList<MatchesBySeasonDto>>.Success(seasons);
    }

    public async Task<Result<DashboardKpiDto>> HandleAsync(
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

        return Result<DashboardKpiDto>.Success(new DashboardKpiDto(countries, leagues, teams, players, matches));
    }

    public async Task<Result<IReadOnlyList<SeasonRatingTrendPointDto>>> HandleAsync(
        GetSeasonRatingTrendQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var matchesQuery = dbContext.Matches.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Season))
        {
            var season = query.Season.Trim();
            matchesQuery = matchesQuery.Where(match => match.Season == season);
        }

        if (query.LeagueId.HasValue && query.LeagueId.Value > 0)
        {
            matchesQuery = matchesQuery.Where(match => match.LeagueId == query.LeagueId.Value);
        }

        var activeMonths = await matchesQuery
            .Where(match => match.Date != default)
            .Select(match => new DateTime(match.Date.Year, match.Date.Month, 1))
            .Distinct()
            .OrderBy(month => month)
            .ToListAsync(cancellationToken);

        if (activeMonths.Count == 0)
        {
            return Result<IReadOnlyList<SeasonRatingTrendPointDto>>.Success(Array.Empty<SeasonRatingTrendPointDto>());
        }

        var monthSet = activeMonths.ToHashSet();
        var trend = await dbContext.PlayerAttributes
            .AsNoTracking()
            .Where(attribute => attribute.OverallRating.HasValue)
            .Select(attribute => new
            {
                Month = new DateTime(attribute.Date.Year, attribute.Date.Month, 1),
                attribute.OverallRating,
            })
            .Where(item => monthSet.Contains(item.Month))
            .GroupBy(item => item.Month)
            .Select(group => new SeasonRatingTrendPointDto(
                group.Key,
                Math.Round(group.Average(item => item.OverallRating!.Value), 2)))
            .OrderBy(item => item.Month)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SeasonRatingTrendPointDto>>.Success(trend);
    }

    public async Task<Result<MatchOutcomeDistributionDto>> HandleAsync(
        GetMatchOutcomeDistributionQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var matchesQuery = dbContext.Matches.AsNoTracking()
            .Where(match => match.HomeTeamGoal.HasValue && match.AwayTeamGoal.HasValue);

        if (!string.IsNullOrWhiteSpace(query.Season))
        {
            var season = query.Season.Trim();
            matchesQuery = matchesQuery.Where(match => match.Season == season);
        }

        if (query.LeagueId.HasValue && query.LeagueId.Value > 0)
        {
            matchesQuery = matchesQuery.Where(match => match.LeagueId == query.LeagueId.Value);
        }

        var rows = await matchesQuery
            .Select(match => new
            {
                Home = match.HomeTeamGoal!.Value,
                Away = match.AwayTeamGoal!.Value,
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return Result<MatchOutcomeDistributionDto>.Success(new MatchOutcomeDistributionDto(0, 0, 0, 0, 0));
        }

        var homeWins = rows.Count(row => row.Home > row.Away);
        var draws = rows.Count(row => row.Home == row.Away);
        var awayWins = rows.Count(row => row.Away > row.Home);
        var avgGoals = Math.Round(rows.Average(row => row.Home + row.Away), 2);

        return Result<MatchOutcomeDistributionDto>.Success(
            new MatchOutcomeDistributionDto(homeWins, draws, awayWins, avgGoals, rows.Count));
    }

    public async Task<Result<IReadOnlyList<LeagueCompetitivenessDto>>> HandleAsync(
        GetLeagueCompetitivenessQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var limit = query.Limit < 1 ? 8 : Math.Min(query.Limit, 30);

        var matchesQuery = dbContext.Matches.AsNoTracking()
            .Where(match => match.HomeTeamGoal.HasValue && match.AwayTeamGoal.HasValue);

        if (!string.IsNullOrWhiteSpace(query.Season))
        {
            var season = query.Season.Trim();
            matchesQuery = matchesQuery.Where(match => match.Season == season);
        }

        var grouped = from match in matchesQuery
                      group match by match.LeagueId
            into byLeague
                      select new
                      {
                          LeagueId = byLeague.Key,
                          MatchCount = byLeague.Count(),
                          DrawCount = byLeague.Count(item => item.HomeTeamGoal == item.AwayTeamGoal),
                          AvgGoalDiff = byLeague.Average(item => Math.Abs(item.HomeTeamGoal!.Value - item.AwayTeamGoal!.Value)),
                      };

        var result = await (from groupRow in grouped
                            join league in dbContext.Leagues.AsNoTracking() on groupRow.LeagueId equals league.Id
                            orderby (double)groupRow.DrawCount / Math.Max(groupRow.MatchCount, 1) descending, groupRow.AvgGoalDiff
                            select new LeagueCompetitivenessDto(
                                league.Id,
                                league.Name,
                                league.CountryName,
                                groupRow.MatchCount,
                                Math.Round((double)groupRow.DrawCount / Math.Max(groupRow.MatchCount, 1), 4),
                                Math.Round(groupRow.AvgGoalDiff, 2)))
            .Take(limit)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LeagueCompetitivenessDto>>.Success(result);
    }
}
