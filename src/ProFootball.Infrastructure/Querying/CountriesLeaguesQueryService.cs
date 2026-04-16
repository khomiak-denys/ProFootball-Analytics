using Microsoft.EntityFrameworkCore;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Querying;

public sealed class CountriesLeaguesQueryService(IDbContextFactory<ProFootballDbContext> dbContextFactory) : ICountriesLeaguesQueryService
{
    public async Task<IReadOnlyList<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Countries
            .AsNoTracking()
            .OrderBy(country => country.Name)
            .Select(country => new CountryDto(country.Id, country.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeagueDto>> GetLeaguesAsync(
        int? countryId = null,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = from league in dbContext.Leagues.AsNoTracking()
                    join country in dbContext.Countries.AsNoTracking() on league.CountryId equals country.Id
                    select new { league, country };

        if (countryId.HasValue)
        {
            query = query.Where(item => item.league.CountryId == countryId.Value);
        }

        return await query
            .OrderBy(item => item.league.Name)
            .Select(item => new LeagueDto(
                item.league.Id,
                item.league.Name,
                item.country.Id,
                item.country.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CountryLeagueListItemDto>> GetCountriesWithLeagueCountAsync(
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

    public async Task<IReadOnlyList<LeagueCountryCardDto>> GetLeagueCardsAsync(
        int countryId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await GetLeagueCardsCoreAsync(dbContext, countryId, cancellationToken);
    }

    public async Task<CountryLeagueSummaryDto> GetCountrySummaryAsync(
        int countryId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var cards = await GetLeagueCardsCoreAsync(dbContext, countryId, cancellationToken);
        var activeLeagues = cards.Count;
        return new CountryLeagueSummaryDto(
            cards.Sum(card => card.TeamsCount),
            activeLeagues,
            activeLeagues);
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
