using ProFootball.Application.Contracts.Queries;

namespace ProFootball.Application.Abstractions.Querying;

public interface ICountriesLeaguesQueryService
{
    Task<IReadOnlyList<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeagueDto>> GetLeaguesAsync(int? countryId = null, CancellationToken cancellationToken = default);
}
