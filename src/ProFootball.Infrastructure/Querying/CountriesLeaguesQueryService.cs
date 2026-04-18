using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Cqrs;
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
    public async Task<IReadOnlyList<CountryDto>> HandleAsync(
        GetCountriesQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Countries
            .AsNoTracking()
            .OrderBy(country => country.Name)
            .Select(country => new CountryDto(country.Id, country.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeagueDto>> HandleAsync(
        GetLeaguesQuery query,
        CancellationToken cancellationToken = default)
    {
        var countryId = query.CountryId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var leaguesQuery = from league in dbContext.Leagues.AsNoTracking()
                           join country in dbContext.Countries.AsNoTracking() on league.CountryId equals country.Id
                           select new { league, country };

        if (countryId.HasValue)
        {
            leaguesQuery = leaguesQuery.Where(item => item.league.CountryId == countryId.Value);
        }

        return await leaguesQuery
            .OrderBy(item => item.league.Name)
            .Select(item => new LeagueDto(
                item.league.Id,
                item.league.Name,
                item.country.Id,
                item.country.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CountryLeagueListItemDto>> HandleAsync(
        GetCountriesWithLeagueCountQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await (from country in dbContext.Countries.AsNoTracking()
                      join league in dbContext.Leagues.AsNoTracking()
                          on country.Id equals league.CountryId into leagues
                      orderby country.Name
                      select new CountryLeagueListItemDto(
                          country.Id,
                          country.Name,
                          leagues.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<CountryLeagueSnapshotDto> HandleAsync(
        GetCountrySnapshotQuery query,
        CancellationToken cancellationToken = default)
    {
        var countryId = query.CountryId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var cards = await GetLeagueCardsCoreAsync(dbContext, countryId, cancellationToken);
        var activeLeagues = cards.Count;
        var summary = new CountryLeagueSummaryDto(
            TotalClubs: cards.Sum(card => card.TeamsCount),
            ActiveLeagues: activeLeagues,
            Divisions: CalculateDivisions(cards));
        return new CountryLeagueSnapshotDto(cards, summary);
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
        int countryId,
        CancellationToken cancellationToken)
    {
        var leagues = await dbContext.Leagues
            .AsNoTracking()
            .Where(league => league.CountryId == countryId)
            .OrderBy(league => league.Name)
            .Select(league => new
            {
                league.Id,
                league.Name,
            })
            .ToListAsync(cancellationToken);

        if (leagues.Count == 0)
        {
            return Array.Empty<LeagueCountryCardDto>();
        }

        var seasonalAggregates = await dbContext.Matches
            .AsNoTracking()
            .Where(match => match.CountryId == countryId)
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
                    .Select(match => match.HomeTeamApiId)
                    .Concat(grouped.Select(match => match.AwayTeamApiId))
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
                        0,
                        0);
                }

                return new LeagueCountryCardDto(
                    league.Id,
                    league.Name,
                    latestAggregate.Season,
                    latestAggregate.TeamsCount,
                    latestAggregate.MatchCount);
            })
            .ToList();
    }
}
