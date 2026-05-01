using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Country.Dtos;
using ProFootball.Application.Country.Queries;
using ProFootball.Application.League.Dtos;
using ProFootball.Application.League.Queries;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class CountriesLeaguesQueryService(IDbContextFactory<ProFootballDbContext> dbContextFactory) :
    IQueryHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>,
    IQueryHandler<GetLeaguesQuery, IReadOnlyList<LeagueDto>>,
    IQueryHandler<GetCountriesWithLeagueCountQuery, IReadOnlyList<CountryLeagueListItemDto>>,
    IQueryHandler<GetCountrySnapshotQuery, CountryLeagueSnapshotDto>
{
    public async Task<Result<IReadOnlyList<CountryDto>>> HandleAsync(
        GetCountriesQuery _,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var countries = await dbContext.Leagues
            .AsNoTracking()
            .Where(league => !string.IsNullOrWhiteSpace(league.CountryName))
            .Select(league => league.CountryName)
            .Distinct()
            .OrderBy(countryName => countryName)
            .Select(countryName => new CountryDto(countryName))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CountryDto>>.Success(countries);
    }

    public async Task<Result<IReadOnlyList<LeagueDto>>> HandleAsync(
        GetLeaguesQuery query,
        CancellationToken cancellationToken = default)
    {
        var countryName = query.CountryName?.Trim();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var leaguesQuery = dbContext.Leagues.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(countryName))
        {
            leaguesQuery = leaguesQuery.Where(league => league.CountryName == countryName);
        }

        var leagues = await leaguesQuery
            .OrderBy(league => league.Name)
            .Select(league => new LeagueDto(
                league.Id,
                league.Name,
                league.CountryName))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LeagueDto>>.Success(leagues);
    }

    public async Task<Result<IReadOnlyList<CountryLeagueListItemDto>>> HandleAsync(
        GetCountriesWithLeagueCountQuery _,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var countries = await dbContext.Leagues
            .AsNoTracking()
            .Where(league => !string.IsNullOrWhiteSpace(league.CountryName))
            .GroupBy(league => league.CountryName)
            .OrderBy(grouped => grouped.Key)
            .Select(grouped => new CountryLeagueListItemDto(
                grouped.Key,
                grouped.Count()))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CountryLeagueListItemDto>>.Success(countries);
    }

    public async Task<Result<CountryLeagueSnapshotDto>> HandleAsync(
        GetCountrySnapshotQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.CountryName))
        {
            return Result<CountryLeagueSnapshotDto>.Success(new CountryLeagueSnapshotDto(
                Array.Empty<LeagueCountryCardDto>(),
                new CountryLeagueSummaryDto(0, 0, 0),
                string.Empty,
                null,
                null,
                Array.Empty<LeagueStandingRowDto>()));
        }

        var countryName = query.CountryName.Trim();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var cards = await GetLeagueCardsCoreAsync(dbContext, countryName, cancellationToken);
        var activeLeagues = cards.Count;
        var summary = new CountryLeagueSummaryDto(
            TotalClubs: cards.Sum(card => card.TeamsCount),
            ActiveLeagues: activeLeagues,
            Divisions: CalculateDivisions(cards));
        var aboutDescription = cards
            .Select(card => card.Description)
            .FirstOrDefault(description => !string.IsNullOrWhiteSpace(description))
            ?? string.Empty;
        var featuredLeague = cards.FirstOrDefault();
        var featuredLeagueStandings = featuredLeague is null
            ? Array.Empty<LeagueStandingRowDto>()
            : await GetLeagueStandingsAsync(
                dbContext,
                featuredLeague.LeagueId,
                featuredLeague.Season,
                cancellationToken);

        return Result<CountryLeagueSnapshotDto>.Success(new CountryLeagueSnapshotDto(
            cards,
            summary,
            aboutDescription,
            featuredLeague?.LeagueName,
            featuredLeague?.Season,
            featuredLeagueStandings));
    }

    private static int CalculateDivisions(IReadOnlyList<LeagueCountryCardDto> cards)
    {
        if (cards.Count == 0)
        {
            return 0;
        }

        return cards
            .Select(card => ResolveLeagueTier(card.LeagueName))
            .DefaultIfEmpty(1)
            .Max();
    }

    private static int ResolveLeagueTier(string leagueName)
    {
        if (string.IsNullOrWhiteSpace(leagueName))
        {
            return 1;
        }

        var normalized = leagueName.Trim().ToLowerInvariant();
        if (normalized.Contains("league two") || normalized.Contains("fourth"))
        {
            return 4;
        }

        if (normalized.Contains("league one") || normalized.Contains("third"))
        {
            return 3;
        }

        if (normalized.Contains("championship")
            || normalized.Contains("segunda")
            || normalized.Contains("serie b")
            || normalized.Contains("ligue 2")
            || normalized.Contains("2. bundesliga"))
        {
            return 2;
        }

        var tokens = normalized.Split([' ', '-', '.', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Contains("4") || tokens.Contains("iv"))
        {
            return 4;
        }

        if (tokens.Contains("3") || tokens.Contains("iii"))
        {
            return 3;
        }

        if (tokens.Contains("2") || tokens.Contains("ii") || tokens.Contains("b"))
        {
            return 2;
        }

        return 1;
    }

    private static async Task<IReadOnlyList<LeagueCountryCardDto>> GetLeagueCardsCoreAsync(
        ProFootballDbContext dbContext,
        string countryName,
        CancellationToken cancellationToken)
    {
        var leagues = await dbContext.Leagues
            .AsNoTracking()
            .Where(league => league.CountryName == countryName)
            .OrderBy(league => league.Name)
            .Select(league => new
            {
                league.Id,
                league.Name,
                league.MaxTeams,
                league.Description,
            })
            .ToListAsync(cancellationToken);

        if (leagues.Count == 0)
        {
            return Array.Empty<LeagueCountryCardDto>();
        }

        var teamCountsByLeague = await dbContext.Teams
            .AsNoTracking()
            .Where(team => team.LeagueId.HasValue)
            .GroupBy(team => team.LeagueId!.Value)
            .Select(grouped => new
            {
                LeagueId = grouped.Key,
                TeamCount = grouped.Count(),
            })
            .ToDictionaryAsync(item => item.LeagueId, item => item.TeamCount, cancellationToken);

        var seasonalAggregates = await dbContext.Matches
            .AsNoTracking()
            .Where(match => match.CountryName == countryName)
            .GroupBy(match => new
            {
                match.LeagueId,
                match.Season,
            })
            .Select(grouped => new
            {
                grouped.Key.LeagueId,
                grouped.Key.Season,
                LatestDate = grouped.Max(match => match.Date),
                MatchCount = grouped.Count(),
                TeamsCount = grouped
                    .Select(match => match.HomeTeamId)
                    .Concat(grouped.Select(match => match.AwayTeamId))
                    .Distinct()
                    .Count(),
            })
            .ToListAsync(cancellationToken);

        var latestByLeague = seasonalAggregates
            .GroupBy(aggregate => aggregate.LeagueId)
            .ToDictionary(
                grouped => grouped.Key,
                grouped => grouped
                    .OrderByDescending(aggregate => aggregate.LatestDate)
                    .ThenByDescending(aggregate => aggregate.Season, StringComparer.Ordinal)
                    .First());

        return leagues
            .Select(league =>
            {
                if (!latestByLeague.TryGetValue(league.Id, out var latestAggregate))
                {
                    return new LeagueCountryCardDto(
                        league.Id,
                        league.Name,
                        "--",
                        teamCountsByLeague.TryGetValue(league.Id, out var teamsWithoutMatches) ? teamsWithoutMatches : 0,
                        0,
                        league.MaxTeams,
                        league.Description);
                }

                var directTeamCount = teamCountsByLeague.TryGetValue(league.Id, out var directTeams) ? directTeams : 0;

                return new LeagueCountryCardDto(
                    league.Id,
                    league.Name,
                    latestAggregate.Season,
                    Math.Max(latestAggregate.TeamsCount, directTeamCount),
                    latestAggregate.MatchCount,
                    league.MaxTeams,
                    league.Description);
            })
            .ToList();
    }

    private static async Task<IReadOnlyList<LeagueStandingRowDto>> GetLeagueStandingsAsync(
        ProFootballDbContext dbContext,
        int leagueId,
        string season,
        CancellationToken cancellationToken)
    {
        var teamSeasonStatsRows = await dbContext.TeamSeasonStats
            .AsNoTracking()
            .Where(stat => stat.LeagueId == leagueId && stat.Season == season)
            .Join(
                dbContext.Teams.AsNoTracking(),
                stat => stat.TeamId,
                team => team.Id,
                (stat, team) => new
                {
                    stat.TeamId,
                    TeamName = string.IsNullOrWhiteSpace(team.LongName) ? (team.ShortName ?? team.Id.ToString()) : team.LongName,
                    stat.Wins,
                    stat.Draws,
                    stat.Losses,
                    stat.GoalsFor,
                    stat.GoalsAgainst,
                    stat.Points,
                })
            .ToListAsync(cancellationToken);

        if (teamSeasonStatsRows.Count > 0)
        {
            return teamSeasonStatsRows
                .OrderByDescending(row => row.Points)
                .ThenByDescending(row => row.GoalsFor - row.GoalsAgainst)
                .ThenByDescending(row => row.GoalsFor)
                .ThenBy(row => row.TeamName)
                .Select((row, index) => new LeagueStandingRowDto(
                    index + 1,
                    row.TeamId,
                    row.TeamName,
                    row.Wins,
                    row.Draws,
                    row.Losses,
                    row.GoalsFor,
                    row.GoalsAgainst,
                    row.Points))
                .ToList();
        }

        var matchRows = await dbContext.Matches
            .AsNoTracking()
            .Where(match => match.LeagueId == leagueId && match.Season == season)
            .Select(match => new
            {
                match.HomeTeamId,
                match.AwayTeamId,
                match.HomeTeamGoal,
                match.AwayTeamGoal,
            })
            .ToListAsync(cancellationToken);

        if (matchRows.Count == 0)
        {
            return Array.Empty<LeagueStandingRowDto>();
        }

        var table = new Dictionary<int, StandingAccumulator>();
        foreach (var match in matchRows)
        {
            if (!table.TryGetValue(match.HomeTeamId, out var home))
            {
                home = new StandingAccumulator();
                table[match.HomeTeamId] = home;
            }

            if (!table.TryGetValue(match.AwayTeamId, out var away))
            {
                away = new StandingAccumulator();
                table[match.AwayTeamId] = away;
            }

            var homeGoals = match.HomeTeamGoal ?? 0;
            var awayGoals = match.AwayTeamGoal ?? 0;

            home.GoalsFor += homeGoals;
            home.GoalsAgainst += awayGoals;
            away.GoalsFor += awayGoals;
            away.GoalsAgainst += homeGoals;

            if (homeGoals > awayGoals)
            {
                home.Wins++;
                away.Losses++;
                home.Points += 3;
            }
            else if (homeGoals < awayGoals)
            {
                away.Wins++;
                home.Losses++;
                away.Points += 3;
            }
            else
            {
                home.Draws++;
                away.Draws++;
                home.Points++;
                away.Points++;
            }
        }

        var teamNames = await dbContext.Teams
            .AsNoTracking()
            .Where(team => table.Keys.Contains(team.Id))
            .ToDictionaryAsync(
                team => team.Id,
                team => string.IsNullOrWhiteSpace(team.LongName) ? (team.ShortName ?? team.Id.ToString()) : team.LongName,
                cancellationToken);

        return table
            .Select(entry =>
            {
                var teamId = entry.Key;
                var stat = entry.Value;
                return new LeagueStandingRowDto(
                    0,
                    teamId,
                    teamNames.GetValueOrDefault(teamId, teamId.ToString()),
                    stat.Wins,
                    stat.Draws,
                    stat.Losses,
                    stat.GoalsFor,
                    stat.GoalsAgainst,
                    stat.Points);
            })
            .OrderByDescending(row => row.Points)
            .ThenByDescending(row => row.GoalsFor - row.GoalsAgainst)
            .ThenByDescending(row => row.GoalsFor)
            .ThenBy(row => row.TeamName)
            .Select((row, index) => row with { Position = index + 1 })
            .ToList();
    }

    private sealed class StandingAccumulator
    {
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }
    }
}
