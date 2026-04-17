using ProFootball.Application.Contracts.Queries;

namespace ProFootball.Application.Abstractions.Querying;

public interface ICountriesLeaguesQueryService
{
    Task<IReadOnlyList<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeagueDto>> GetLeaguesAsync(int? countryId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CountryLeagueListItemDto>> GetCountriesWithLeagueCountAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeagueCountryCardDto>> GetLeagueCardsAsync(
        int countryId,
        CancellationToken cancellationToken = default);

    Task<CountryLeagueSnapshotDto> GetCountrySnapshotAsync(
        int countryId,
        CancellationToken cancellationToken = default);
}
