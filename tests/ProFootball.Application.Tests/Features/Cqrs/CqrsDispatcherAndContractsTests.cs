using Microsoft.Extensions.DependencyInjection;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Country.Dtos;
using ProFootball.Application.Country.Queries;
using ProFootball.Application.Dispatching;
using ProFootball.Application.Importing.Commands;
using ProFootball.Application.Importing.Dtos;
using ProFootball.Application.League.Dtos;
using ProFootball.Application.League.Queries;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Application.Player.Dtos;
using ProFootball.Application.Player.Queries;
using ProFootball.Application.Team.Dtos;
using ProFootball.Application.Team.Queries;
using Xunit;

namespace ProFootball.Application.Tests.Features.Cqrs;

public class CqrsDispatcherAndContractsTests
{
    [Fact]
    public async Task QueryDispatcher_ShouldResolveAndInvokeHandler()
    {
        var services = new ServiceCollection();
        services.AddScoped<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        using var rootProvider = services.BuildServiceProvider();
        await using var scope = rootProvider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var result = await dispatcher.DispatchResultAsync<TestQuery, int>(new TestQuery(5));

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public async Task CommandDispatcher_ShouldResolveAndInvokeHandlers()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddScoped<ICommandHandler<TestResultCommand, string>, TestResultCommandHandler>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        using var rootProvider = services.BuildServiceProvider();
        await using var scope = rootProvider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        await dispatcher.DispatchAsync(new TestCommand());
        var result = await dispatcher.DispatchAsync<TestResultCommand, string>(new TestResultCommand("ok"));

        Assert.Equal("OK", result);
    }

    [Fact]
    public void Contracts_ShouldRetainAssignedValues()
    {
        var today = DateTime.UtcNow.Date;

        var country = new CountryDto("England");
        var countryListItem = new CountryLeagueListItemDto("England", 2);
        var summary = new CountryLeagueSummaryDto(40, 2, 2);
        var leagueCard = new LeagueCountryCardDto(1, "Premier League", "2025/26", 20, 380, 20, "Top division");
        var snapshot = new CountryLeagueSnapshotDto(
            [leagueCard],
            summary,
            "Top division",
            "Premier League",
            "2024/2025",
            []);
        var league = new LeagueDto(1, "Premier League", "England");

        var team = new TeamListItemDto(100, "Arsenal", "ARS", 1000);
        var teamAttribute = new TeamAttributeDto(today, 70, 71, 72, 73);
        var teamDetails = new TeamDetailsDto(team, [teamAttribute]);
        var teamSearch = new TeamSearchQuery("ars", "shortname", false, 2, 15);
        var teamDetailsQuery = new GetTeamDetailsQuery(100);

        var player = new PlayerListItemDto(9001, "Kevin", "De Bruyne", today.AddYears(-30), 181, 76, 91, 92, "Right");
        var playerAttribute = new PlayerAttributeDto(today, 91, 92, "Right", "high", "medium");
        var playerDetails = new PlayerDetailsDto(9001, 5001, "Kevin", "De Bruyne", today.AddYears(-30), 181, 76, [playerAttribute]);
        var trendPoint = new PlayerTrendPointDto(today, 91, 92);
        var topPlayer = new TopPlayerDto(9001, "Kevin", 90.5, 91.3, 25);
        var playerSearch = new PlayerSearchQuery("kev", 80, null, null, null, null, null, null, "overallrating", true, 1, 20);
        var playerDetailsQuery = new GetPlayerDetailsQuery(9001);
        var playerTrendQuery = new GetPlayerTrendQuery(9001);
        var topPlayersQuery = new TopPlayersQuery(10, 85, 88, "Right", "overall", true);

        var match = new MatchListItemDto(5001, today, "2025/26", "Premier League", "England", 100, "Arsenal", 200, "Chelsea", 2, 1);
        var matchDetails = new MatchDetailsDto(5001, today, "2025/26", "Premier League", "England", 100, "Arsenal", 200, "Chelsea", 2, 1, 18, 3, 0, 6, 17, 2, 13, 87);
        var bySeason = new MatchesBySeasonDto("2025/26", "Premier League", 380);
        var kpi = new DashboardKpiDto(5, 10, 20, 500, 3800);
        var matchSearch = new MatchSearchQuery(1, "2025/26", 100, null, null, "date", true, 1, 25);
        var matchDetailsQuery = new GetMatchDetailsQuery(5001);
        var matchesBySeasonQuery = new GetMatchesBySeasonQuery(1);
        var dashboardQuery = new GetDashboardKpiQuery();

        var leaguesQuery = new GetLeaguesQuery("England");
        var countriesQuery = new GetCountriesQuery();
        var countriesWithCountQuery = new GetCountriesWithLeagueCountQuery();
        var snapshotQuery = new GetCountrySnapshotQuery("England");
        var importCommand = new ImportDataCommand("db.sqlite", 1000);
        var importResult = new DataImportResult(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0, 12, TimeSpan.FromSeconds(1));

        Assert.Equal("England", country.Name);
        Assert.Equal(2, countryListItem.LeagueCount);
        Assert.Equal(40, summary.TotalClubs);
        Assert.Equal("Premier League", leagueCard.LeagueName);
        Assert.Single(snapshot.LeagueCards);
        Assert.Equal("Premier League", league.Name);
        Assert.Equal("England", league.CountryName);

        Assert.Equal("Arsenal", team.LongName);
        Assert.Equal(100, team.TeamApiId);
        Assert.Equal("ARS", team.ShortName);
        Assert.Equal(1000, team.TeamFifaApiId);
        Assert.Equal(today, teamAttribute.Date);
        Assert.Equal(70, teamAttribute.BuildUpPlaySpeed);
        Assert.Equal(71, teamAttribute.BuildUpPlayPassing);
        Assert.Equal(72, teamAttribute.ChanceCreationPassing);
        Assert.Equal(73, teamAttribute.DefencePressure);
        Assert.Equal(team, teamDetails.Team);
        Assert.Single(teamDetails.Attributes);
        Assert.Equal("ars", teamSearch.Name);
        Assert.Equal("shortname", teamSearch.SortBy);
        Assert.False(teamSearch.SortDescending);
        Assert.Equal(2, teamSearch.Page);
        Assert.Equal(15, teamSearch.PageSize);
        Assert.Equal(100, teamDetailsQuery.TeamApiId);

        Assert.Equal(9001, player.PlayerApiId);
        Assert.Equal("Kevin", player.FirstName);
        Assert.Equal("De Bruyne", player.LastName);
        Assert.Equal(181, player.Height);
        Assert.Equal(76, player.Weight);
        Assert.Equal(91, player.OverallRating);
        Assert.Equal(92, player.Potential);
        Assert.Equal("Right", player.PreferredFoot);
        Assert.Equal(today, playerAttribute.Date);
        Assert.Equal(91, playerAttribute.OverallRating);
        Assert.Equal(92, playerAttribute.Potential);
        Assert.Equal("Right", playerAttribute.PreferredFoot);
        Assert.Equal("high", playerAttribute.AttackingWorkRate);
        Assert.Equal("medium", playerAttribute.DefensiveWorkRate);
        Assert.Equal(5001, playerDetails.PlayerFifaApiId);
        Assert.Equal("Kevin", playerDetails.FirstName);
        Assert.Equal("De Bruyne", playerDetails.LastName);
        Assert.Equal(181, playerDetails.Height);
        Assert.Equal(76, playerDetails.Weight);
        Assert.Single(playerDetails.Attributes);
        Assert.Equal(91, trendPoint.OverallRating);
        Assert.Equal(92, trendPoint.Potential);
        Assert.Equal(today, trendPoint.Date);
        Assert.Equal("Kevin", topPlayer.PlayerName);
        Assert.Equal(90.5, topPlayer.AverageOverallRating);
        Assert.Equal(91.3, topPlayer.AveragePotential);
        Assert.Equal(25, topPlayer.Samples);
        Assert.Equal("kev", playerSearch.Name);
        Assert.Equal(80, playerSearch.MinOverallRating);
        Assert.Null(playerSearch.MaxOverallRating);
        Assert.Null(playerSearch.MinPotential);
        Assert.Null(playerSearch.MaxPotential);
        Assert.Null(playerSearch.MinHeight);
        Assert.Null(playerSearch.MaxHeight);
        Assert.Null(playerSearch.PreferredFoot);
        Assert.Equal("overallrating", playerSearch.SortBy);
        Assert.True(playerSearch.SortDescending);
        Assert.Equal(1, playerSearch.Page);
        Assert.Equal(20, playerSearch.PageSize);
        Assert.Equal(9001, playerDetailsQuery.PlayerApiId);
        Assert.Equal(9001, playerTrendQuery.PlayerApiId);
        Assert.Equal(10, topPlayersQuery.Limit);
        Assert.Equal(85, topPlayersQuery.MinOverallRating);
        Assert.Equal(88, topPlayersQuery.MinPotential);
        Assert.Equal("Right", topPlayersQuery.PreferredFoot);
        Assert.Equal("overall", topPlayersQuery.SortBy);
        Assert.True(topPlayersQuery.SortDescending);

        Assert.Equal(5001, match.MatchApiId);
        Assert.Equal(today, match.Date);
        Assert.Equal("2025/26", match.Season);
        Assert.Equal("England", match.CountryName);
        Assert.Equal(100, match.HomeTeamApiId);
        Assert.Equal("Arsenal", match.HomeTeamName);
        Assert.Equal(200, match.AwayTeamApiId);
        Assert.Equal("Chelsea", match.AwayTeamName);
        Assert.Equal(2, match.HomeTeamGoal);
        Assert.Equal(1, match.AwayTeamGoal);
        Assert.Equal("Premier League", matchDetails.LeagueName);
        Assert.Equal(today, matchDetails.Date);
        Assert.Equal("2025/26", matchDetails.Season);
        Assert.Equal("England", matchDetails.CountryName);
        Assert.Equal(100, matchDetails.HomeTeamApiId);
        Assert.Equal("Arsenal", matchDetails.HomeTeamName);
        Assert.Equal(200, matchDetails.AwayTeamApiId);
        Assert.Equal("Chelsea", matchDetails.AwayTeamName);
        Assert.Equal(2, matchDetails.HomeTeamGoal);
        Assert.Equal(1, matchDetails.AwayTeamGoal);
        Assert.Equal("2025/26", bySeason.Season);
        Assert.Equal("Premier League", bySeason.LeagueName);
        Assert.Equal(380, bySeason.MatchCount);
        Assert.Equal(5, kpi.Countries);
        Assert.Equal(10, kpi.Leagues);
        Assert.Equal(20, kpi.Teams);
        Assert.Equal(500, kpi.Players);
        Assert.Equal(3800, kpi.Matches);
        Assert.Equal(1, matchSearch.LeagueId);
        Assert.Equal("2025/26", matchSearch.Season);
        Assert.Equal(100, matchSearch.TeamApiId);
        Assert.Null(matchSearch.DateFrom);
        Assert.Null(matchSearch.DateTo);
        Assert.Equal("date", matchSearch.SortBy);
        Assert.True(matchSearch.SortDescending);
        Assert.Equal(1, matchSearch.Page);
        Assert.Equal(25, matchSearch.PageSize);
        Assert.Equal(5001, matchDetailsQuery.MatchApiId);
        Assert.Equal(1, matchesBySeasonQuery.LeagueId);
        Assert.NotNull(dashboardQuery);

        Assert.Equal("England", leaguesQuery.CountryName);
        Assert.NotNull(countriesQuery);
        Assert.NotNull(countriesWithCountQuery);
        Assert.Equal("England", snapshotQuery.CountryName);
        Assert.Equal("db.sqlite", importCommand.SqlitePath);
        Assert.Equal(1000, importCommand.BatchSize);
        Assert.Equal("realistic", importCommand.Profile);
        Assert.Equal("regenerate", importCommand.Mode);
        Assert.Equal(1, importResult.CountriesResolved);
        Assert.Equal(2, importResult.LeaguesImported);
        Assert.Equal(3, importResult.TeamsImported);
        Assert.Equal(4, importResult.PlayersImported);
        Assert.Equal(5, importResult.MatchesImported);
        Assert.Equal(6, importResult.TeamAttributesImported);
        Assert.Equal(7, importResult.PlayerAttributesImported);
        Assert.Equal(8, importResult.MatchEventsGenerated);
        Assert.Equal(9, importResult.PlayerMatchStatsGenerated);
        Assert.Equal(10, importResult.TeamSeasonStatsRebuilt);
        Assert.Equal(0, importResult.ValidationErrors);
        Assert.Equal(12, importResult.SkippedRows);
        Assert.Equal(TimeSpan.FromSeconds(1), importResult.Duration);

        var paged = new PagedResult<int>([1, 2], 2, 1, 20);
        Assert.Equal(2, paged.Items.Count);
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(1, paged.Page);
        Assert.Equal(20, paged.PageSize);
    }

    private sealed record TestQuery(int Value) : IQuery<int>;
    private sealed class TestQueryHandler : IQueryHandler<TestQuery, int>
    {
        public Task<Result<int>> HandleAsync(TestQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<int>.Success(query.Value * 2));
    }

    private sealed record TestCommand : ICommand;
    private sealed class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public Task HandleAsync(TestCommand command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record TestResultCommand(string Value) : ICommand<string>;
    private sealed class TestResultCommandHandler : ICommandHandler<TestResultCommand, string>
    {
        public Task<string> HandleAsync(TestResultCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(command.Value.ToUpperInvariant());
    }
}
