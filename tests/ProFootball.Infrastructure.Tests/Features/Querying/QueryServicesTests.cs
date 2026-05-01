using ProFootball.Application.Country.Queries;
using ProFootball.Application.Common;
using ProFootball.Application.League.Queries;
using ProFootball.Application.Match.Queries;
using ProFootball.Application.Player.Queries;
using ProFootball.Application.Team.Queries;
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

        var firstPageResult = await service.HandleAsync(new TeamSearchQuery(
            Name: null,
            SortBy: "teamapiid",
            SortDescending: true,
            Page: 1,
            PageSize: 2));
        var firstPage = AssertSuccess(firstPageResult);

        var secondPageResult = await service.HandleAsync(new TeamSearchQuery(
            Name: null,
            SortBy: "teamapiid",
            SortDescending: true,
            Page: 2,
            PageSize: 2));
        var secondPage = AssertSuccess(secondPageResult);

        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(new[] { 4, 3 }, firstPage.Items.Select(item => item.TeamApiId));
        Assert.Equal(new[] { 2, 1 }, secondPage.Items.Select(item => item.TeamApiId));
    }

    [Fact]
    public async Task Teams_GetDetails_ShouldReturnNull_WhenTeamDoesNotExist()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new TeamsQueryService(host.DbContextFactory);

        var detailsResult = await service.HandleAsync(new GetTeamDetailsQuery(999999));
        var details = AssertSuccess(detailsResult);

        Assert.Null(details);
    }

    [Fact]
    public async Task Players_Search_ShouldSortByOverallRating_AndKeepUnratedPlayersLast()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new PlayersQueryService(host.DbContextFactory);

        var resultValue = await service.HandleAsync(new PlayerSearchQuery(
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
        var result = AssertSuccess(resultValue);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.Items[0].PlayerApiId);
        Assert.Equal(3, result.Items[^1].PlayerApiId);
        Assert.Null(result.Items[^1].OverallRating);
    }

    [Fact]
    public async Task Players_Search_ShouldApplyCaseInsensitiveNameFilter_InSqliteFallback()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new PlayersQueryService(host.DbContextFactory);

        var resultValue = await service.HandleAsync(new PlayerSearchQuery(
            Name: "kEvIn",
            MinOverallRating: null,
            MaxOverallRating: null,
            MinPotential: null,
            MaxPotential: null,
            MinHeight: null,
            MaxHeight: null,
            PreferredFoot: null,
            SortBy: "height",
            SortDescending: false,
            Page: 1,
            PageSize: 10));
        var result = AssertSuccess(resultValue);

        var player = Assert.Single(result.Items);
        Assert.Equal(1, player.PlayerApiId);
    }

    [Fact]
    public async Task Players_GetDetails_ShouldReturnAttributesOrderedByDateThenIdDesc()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new PlayersQueryService(host.DbContextFactory);

        var detailsResult = await service.HandleAsync(new GetPlayerDetailsQuery(2));
        var details = AssertSuccess(detailsResult);

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

        var resultValue = await service.HandleAsync(new MatchSearchQuery(
            LeagueId: 10,
            Season: "2024/2025",
            TeamApiId: 1,
            DateFrom: new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTo: new DateTime(2024, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            SortBy: "date",
            SortDescending: false,
            Page: 1,
            PageSize: 10));
        var result = AssertSuccess(resultValue);

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].MatchApiId);
    }

    [Fact]
    public async Task Matches_GetMatchTeams_ShouldReturnDistinctTeamsForLeague_SortedByName()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new MatchesQueryService(host.DbContextFactory);

        var teamsResult = await service.HandleAsync(new GetMatchTeamsQuery(10));
        var teams = AssertSuccess(teamsResult);

        Assert.Equal(2, teams.Count);
        Assert.Equal(new[] { 1, 3 }, teams.Select(team => team.TeamApiId));
        Assert.Equal(new[] { "Arsenal", "Chelsea" }, teams.Select(team => team.LongName));
    }

    [Fact]
    public async Task Matches_GetMatchTeams_ShouldReturnDistinctTeamsAcrossAllLeagues_WhenLeagueIsNotSpecified()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new MatchesQueryService(host.DbContextFactory);

        var teamsResult = await service.HandleAsync(new GetMatchTeamsQuery());
        var teams = AssertSuccess(teamsResult);

        Assert.Equal(4, teams.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, teams.Select(team => team.TeamApiId).OrderBy(id => id));
    }

    [Fact]
    public async Task Matches_GetDetails_ShouldReturnNull_WhenMatchDoesNotExist()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new MatchesQueryService(host.DbContextFactory);

        var detailsResult = await service.HandleAsync(new GetMatchDetailsQuery(77777));
        var details = AssertSuccess(detailsResult);

        Assert.Null(details);
    }

    [Fact]
    public async Task CountriesLeagues_GetLeagues_ShouldFilterByCountry()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new CountriesLeaguesQueryService(host.DbContextFactory);

        var leaguesResult = await service.HandleAsync(new GetLeaguesQuery("England"));
        var leagues = AssertSuccess(leaguesResult);

        Assert.Equal(2, leagues.Count);
        Assert.All(leagues, league => Assert.Equal("England", league.CountryName));
    }

    [Fact]
    public async Task CountriesLeagues_GetCountriesWithLeagueCount_ShouldReturnDistinctCountries()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new CountriesLeaguesQueryService(host.DbContextFactory);

        var countriesResult = await service.HandleAsync(new GetCountriesWithLeagueCountQuery());
        var countries = AssertSuccess(countriesResult);
        var england = Assert.Single(countries.Where(country => country.Name == "England"));
        var spain = Assert.Single(countries.Where(country => country.Name == "Spain"));

        Assert.Equal(2, england.LeagueCount);
        Assert.Equal(1, spain.LeagueCount);
    }

    [Fact]
    public async Task CountriesLeagues_GetCountrySnapshot_ShouldReturnZeroSummary_WhenCountryHasNoLeagues()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new CountriesLeaguesQueryService(host.DbContextFactory);

        var snapshotResult = await service.HandleAsync(new GetCountrySnapshotQuery("Emptyland"));
        var snapshot = AssertSuccess(snapshotResult);

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

        var snapshotResult = await service.HandleAsync(new GetCountrySnapshotQuery("England"));
        var snapshot = AssertSuccess(snapshotResult);

        Assert.Equal(2, snapshot.LeagueCards.Count);
        Assert.Equal(4, snapshot.Summary.TotalClubs);
        Assert.Equal(2, snapshot.Summary.ActiveLeagues);
        Assert.Equal(2, snapshot.Summary.Divisions);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.FeaturedLeagueName));
        Assert.NotEmpty(snapshot.FeaturedLeagueStandings);
        Assert.All(snapshot.FeaturedLeagueStandings, row => Assert.True(row.Points >= 0));
    }

    [Fact]
    public async Task Analytics_GetTopPlayers_ShouldClampLimitAndSortBySamples()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var service = new AnalyticsQueryService(host.DbContextFactory);

        var topPlayersResult = await service.HandleAsync(new TopPlayersQuery(
            Limit: 500,
            MinOverallRating: null,
            MinPotential: null,
            PreferredFoot: null,
            SortBy: "samples",
            SortDescending: true));
        var topPlayers = AssertSuccess(topPlayersResult);

        Assert.Equal(2, topPlayers.Count);
        Assert.Equal(2, topPlayers[0].PlayerApiId);
        Assert.True(topPlayers[0].Samples >= topPlayers[1].Samples);
    }

    [Fact]
    public async Task Analytics_GetMatchesBySeason_ShouldRespectOptionalLeagueFilter()
    {
        await using var host = await QueryingTestHost.CreateAsync();
        var matchService = new MatchesQueryService(host.DbContextFactory);
        var allLeaguesResult = await matchService.HandleAsync(new GetMatchesBySeasonQuery());
        var filteredResult = await matchService.HandleAsync(new GetMatchesBySeasonQuery(10));
        var allLeagues = AssertSuccess(allLeaguesResult);
        var filtered = AssertSuccess(filteredResult);

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
        var service = new MatchesQueryService(host.DbContextFactory);
        var kpisResult = await service.HandleAsync(new GetDashboardKpiQuery());
        var kpis = AssertSuccess(kpisResult);

        Assert.Equal(2, kpis.Countries);
        Assert.Equal(3, kpis.Leagues);
        Assert.Equal(4, kpis.Teams);
        Assert.Equal(3, kpis.Players);
        Assert.Equal(3, kpis.Matches);
    }

    private static TValue AssertSuccess<TValue>(Result<TValue> result)
    {
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
