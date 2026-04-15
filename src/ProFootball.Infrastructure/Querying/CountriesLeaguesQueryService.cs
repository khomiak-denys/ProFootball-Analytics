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
}
