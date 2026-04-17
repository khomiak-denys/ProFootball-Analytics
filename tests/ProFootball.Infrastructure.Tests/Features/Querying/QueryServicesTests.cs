using ProFootball.Application.Contracts.Analytics;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Infrastructure.Querying;
using Xunit;

namespace ProFootball.Infrastructure.Tests.Features.Querying;

public class QueryServicesTests
{
    [Fact]
    public async Task Teams_Search_ShouldApplySortingAndPaging()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new TeamsQueryService(host.DbContextFactory);

        var firstPage = await service.SearchTeamsAsync(new TeamSearchQuery(
            Name: null,
            SortBy: "teamapiid",
            SortDescending: true,
            Page: 1,
            PageSize: 2));

        var secondPage = await service.SearchTeamsAsync(new TeamSearchQuery(
            Name: null,
            SortBy: "teamapiid",
            SortDescending: true,
            Page: 2,
            PageSize: 2));

        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(new[] { 400, 300 }, firstPage.Items.Select(item => item.TeamApiId));
        Assert.Equal(new[] { 200, 100 }, secondPage.Items.Select(item => item.TeamApiId));
    }

    [Fact]
    public async Task Teams_GetDetails_ShouldReturnNull_WhenTeamDoesNotExist()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new TeamsQueryService(host.DbContextFactory);

        var details = await service.GetTeamDetailsAsync(999999);

        Assert.Null(details);
    }

    [Fact]
    public async Task Players_Search_ShouldSortByOverallRating_AndKeepUnratedPlayersLast()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new PlayersQueryService(host.DbContextFactory);

        var result = await service.SearchPlayersAsync(new PlayerSearchQuery(
            Name: null,
            MinOverallRating: null,
            MaxOverallRating: null,
            MinPotential: null,
            MaxPotential: null,
            MinHeight: null,
            MaxHeight: null,
            PreferredFoot: null,
            SortBy: "overallrating",
            SortDescending: true,
            Page: 1,
            PageSize: 10));

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(9001, result.Items[0].PlayerApiId);
        Assert.Equal(9003, result.Items[^1].PlayerApiId);
        Assert.Null(result.Items[^1].OverallRating);
    }

    [Fact]
    public async Task Players_GetDetails_ShouldReturnAttributesOrderedByDateThenIdDesc()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new PlayersQueryService(host.DbContextFactory);

        var details = await service.GetPlayerDetailsAsync(9002);

        Assert.NotNull(details);
        Assert.Equal(3, details!.Attributes.Count);
        Assert.Equal(90, details.Attributes[0].OverallRating);
        Assert.Equal(91, details.Attributes[1].OverallRating);
        Assert.Equal(89, details.Attributes[2].OverallRating);
    }

    [Fact]
    public async Task Matches_Search_ShouldApplyLeagueSeasonTeamAndDateFilters()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new MatchesQueryService(host.DbContextFactory);

        var result = await service.SearchMatchesAsync(new MatchSearchQuery(
            LeagueId: 10,
            Season: "2024/2025",
            TeamApiId: 100,
            DateFrom: new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTo: new DateTime(2024, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            SortBy: "date",
            SortDescending: false,
            Page: 1,
            PageSize: 10));

        Assert.Single(result.Items);
        Assert.Equal(5001, result.Items[0].MatchApiId);
    }

    [Fact]
    public async Task Matches_GetDetails_ShouldReturnNull_WhenMatchDoesNotExist()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new MatchesQueryService(host.DbContextFactory);

        var details = await service.GetMatchDetailsAsync(77777);

        Assert.Null(details);
    }

    [Fact]
    public async Task CountriesLeagues_GetLeagues_ShouldFilterByCountry()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new CountriesLeaguesQueryService(host.DbContextFactory);

        var leagues = await service.GetLeaguesAsync(countryId: 1);

        Assert.Equal(2, leagues.Count);
        Assert.All(leagues, league => Assert.Equal(1, league.CountryId));
    }

    [Fact]
    public async Task CountriesLeagues_GetCountriesWithLeagueCount_ShouldIncludeCountriesWithoutLeagues()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new CountriesLeaguesQueryService(host.DbContextFactory);

        var countries = await service.GetCountriesWithLeagueCountAsync();
        var emptyCountry = Assert.Single(countries.Where(country => country.Id == 3));

        Assert.Equal(0, emptyCountry.LeagueCount);
    }

    [Fact]
    public async Task CountriesLeagues_GetCountrySnapshot_ShouldReturnZeroSummary_WhenCountryHasNoLeagues()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new CountriesLeaguesQueryService(host.DbContextFactory);

        var snapshot = await service.GetCountrySnapshotAsync(3);

        Assert.Empty(snapshot.LeagueCards);
        Assert.Equal(0, snapshot.Summary.TotalClubs);
        Assert.Equal(0, snapshot.Summary.ActiveLeagues);
        Assert.Equal(0, snapshot.Summary.Divisions);
    }

    [Fact]
    public async Task CountriesLeagues_GetCountrySnapshot_ShouldCalculateSummaryAndDivisions()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new CountriesLeaguesQueryService(host.DbContextFactory);

        var snapshot = await service.GetCountrySnapshotAsync(1);

        Assert.Equal(2, snapshot.LeagueCards.Count);
        Assert.Equal(4, snapshot.Summary.TotalClubs);
        Assert.Equal(2, snapshot.Summary.ActiveLeagues);
        Assert.Equal(2, snapshot.Summary.Divisions);
    }

    [Fact]
    public async Task Analytics_GetTopPlayers_ShouldClampLimitAndSortBySamples()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new AnalyticsQueryService(host.DbContextFactory);

        var topPlayers = await service.GetTopPlayersAsync(new TopPlayersQuery(
            Limit: 500,
            MinOverallRating: null,
            MinPotential: null,
            PreferredFoot: null,
            SortBy: "samples",
            SortDescending: true));

        Assert.Equal(2, topPlayers.Count);
        Assert.Equal(9002, topPlayers[0].PlayerApiId);
        Assert.True(topPlayers[0].Samples >= topPlayers[1].Samples);
    }

    [Fact]
    public async Task Analytics_GetMatchesBySeason_ShouldRespectOptionalLeagueFilter()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new AnalyticsQueryService(host.DbContextFactory);

        var allLeagues = await service.GetMatchesBySeasonAsync();
        var filtered = await service.GetMatchesBySeasonAsync(leagueId: 10);

        Assert.Equal(3, allLeagues.Count);
        var single = Assert.Single(filtered);
        Assert.Equal("2024/2025", single.Season);
        Assert.Equal("Premier League", single.LeagueName);
        Assert.Equal(1, single.MatchCount);
    }

    [Fact]
    public async Task Dashboard_GetKpis_ShouldReturnSeededCounts()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new DashboardQueryService(host.DbContextFactory);

        var kpis = await service.GetKpisAsync();

        Assert.Equal(3, kpis.Countries);
        Assert.Equal(3, kpis.Leagues);
        Assert.Equal(4, kpis.Teams);
        Assert.Equal(3, kpis.Players);
        Assert.Equal(3, kpis.Matches);
    }
}
