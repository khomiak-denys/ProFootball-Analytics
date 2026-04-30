using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Dtos;
using ProFootball.Application.Auth.Handlers;
using ProFootball.Application.Match.Commands;
using ProFootball.Application.Match.Handlers;
using ProFootball.Application.League.Commands;
using ProFootball.Application.League.Handlers;
using ProFootball.Application.Player.Commands;
using ProFootball.Application.Player.Handlers;
using ProFootball.Application.Team.Commands;
using ProFootball.Application.Team.Handlers;
using ProFootball.Domain.Entities;
using Xunit;
using PlayerEntity = ProFootball.Domain.Entities.Player;
using MatchEntity = ProFootball.Domain.Entities.FootballMatch;
using TeamEntity = ProFootball.Domain.Entities.Team;
using LeagueEntity = ProFootball.Domain.Entities.League;

namespace ProFootball.Application.Tests.Features.Auth;

public class RbacWriteHandlersTests
{
    [Theory]
    [InlineData("Admin", true)]
    [InlineData("Manager", true)]
    [InlineData("Analyst", false)]
    public async Task PlayerCreate_ShouldRespectRoleMatrix(string role, bool shouldSucceed)
    {
        var store = BuildStore(role);
        var repo = new InMemoryPlayerRepository();
        var handler = new CreatePlayerCommandHandler(repo, store);

        var result = await handler.HandleAsync(new CreatePlayerCommand("Test Player", null, 180, 75));

        Assert.Equal(shouldSucceed, result.IsSuccess);
        Assert.Equal(shouldSucceed ? 1 : 0, repo.Players.Count);
    }

    [Fact]
    public async Task PlayerUpdate_ShouldReturnNotFound_WhenMissing()
    {
        var handler = new UpdatePlayerCommandHandler(new InMemoryPlayerRepository(), BuildStore("Admin"));
        var result = await handler.HandleAsync(new UpdatePlayerCommand(999, "Name", null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("player.not_found", result.Error.Code);
    }

    [Fact]
    public async Task PlayerDelete_ShouldReturnForbidden_ForAnalyst()
    {
        var repo = new InMemoryPlayerRepository();
        await repo.AddAsync(new PlayerEntity(1, "P", null, null, null));
        var handler = new DeletePlayerCommandHandler(repo, BuildStore("Analyst"));

        var result = await handler.HandleAsync(new DeletePlayerCommand(1));

        Assert.True(result.IsFailure);
        Assert.Equal("auth.forbidden", result.Error.Code);
        Assert.Single(repo.Players);
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("Manager", true)]
    [InlineData("Analyst", false)]
    public async Task MatchCreate_ShouldRespectRoleMatrix(string role, bool shouldSucceed)
    {
        var store = BuildStore(role);
        var repo = new InMemoryMatchRepository();
        var handler = new CreateMatchCommandHandler(repo, store);

        var result = await handler.HandleAsync(new CreateMatchCommand("Ukraine", 1, "2015/2016", DateTime.UtcNow.Date, 10, 20, 2, 1));

        Assert.Equal(shouldSucceed, result.IsSuccess);
        Assert.Equal(shouldSucceed ? 1 : 0, repo.Matches.Count);
    }

    [Fact]
    public async Task MatchUpdate_ShouldReturnNotFound_WhenMissing()
    {
        var handler = new UpdateMatchCommandHandler(new InMemoryMatchRepository(), BuildStore("Admin"));
        var result = await handler.HandleAsync(new UpdateMatchCommand(123, "Ukraine", 1, "2015/2016", DateTime.UtcNow.Date, 10, 20, 1, 1));

        Assert.True(result.IsFailure);
        Assert.Equal("match.not_found", result.Error.Code);
    }

    [Fact]
    public async Task MatchDelete_ShouldRemoveEntity_ForAdmin()
    {
        var repo = new InMemoryMatchRepository();
        await repo.AddAsync(new MatchEntity(5, "Ukraine", 1, "2015/2016", DateTime.UtcNow.Date, 10, 20, 0, 0));
        var handler = new DeleteMatchCommandHandler(repo, BuildStore("Admin"));

        var result = await handler.HandleAsync(new DeleteMatchCommand(5));

        Assert.True(result.IsSuccess);
        Assert.Empty(repo.Matches);
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("Manager", true)]
    [InlineData("Analyst", false)]
    public async Task TeamCreate_ShouldRespectRoleMatrix(string role, bool shouldSucceed)
    {
        var store = BuildStore(role);
        var repo = new InMemoryTeamRepository();
        var handler = new CreateTeamCommandHandler(repo, store);

        var result = await handler.HandleAsync(new CreateTeamCommand("FC Test", "FCT"));

        Assert.Equal(shouldSucceed, result.IsSuccess);
        Assert.Equal(shouldSucceed ? 1 : 0, repo.Teams.Count);
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("Manager", true)]
    [InlineData("Analyst", false)]
    public async Task LeagueCreate_ShouldRespectRoleMatrix(string role, bool shouldSucceed)
    {
        var store = BuildStore(role);
        var repo = new InMemoryLeagueRepository();
        var handler = new CreateLeagueCommandHandler(repo, store);

        var result = await handler.HandleAsync(new CreateLeagueCommand("Ukraine", "Premier League", 16, "Top level"));

        Assert.Equal(shouldSucceed, result.IsSuccess);
        Assert.Equal(shouldSucceed ? 1 : 0, repo.Leagues.Count);
    }

    [Fact]
    public async Task LeagueCreate_ShouldValidateMaxTeams()
    {
        var repo = new InMemoryLeagueRepository();
        var handler = new CreateLeagueCommandHandler(repo, BuildStore("Admin"));

        var result = await handler.HandleAsync(new CreateLeagueCommand("Ukraine", "Premier League", 0, "Top level"));

        Assert.True(result.IsFailure);
        Assert.Equal("league.validation", result.Error.Code);
        Assert.Empty(repo.Leagues);
    }

    private static IUserSessionStore BuildStore(string role)
    {
        var store = new InMemorySessionStore();
        store.SetCurrentUser("u", "User", role);
        return store;
    }

    private sealed class InMemoryPlayerRepository : IPlayerRepository
    {
        public List<PlayerEntity> Players { get; } = [];

        public Task<int> GetNextIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(Players.Count == 0 ? 1 : Players.Max(p => p.Id) + 1);
        public Task<PlayerEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Players.FirstOrDefault(p => p.Id == id));
        public Task AddAsync(PlayerEntity player, CancellationToken cancellationToken = default) { Players.Add(player); return Task.CompletedTask; }
        public Task UpdateAsync(PlayerEntity player, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(int id, CancellationToken cancellationToken = default) { Players.RemoveAll(p => p.Id == id); return Task.CompletedTask; }
        public Task AddRangeAsync(IReadOnlyCollection<PlayerEntity> players, CancellationToken cancellationToken = default) { Players.AddRange(players); return Task.CompletedTask; }
        public Task DeleteAllAsync(CancellationToken cancellationToken = default) { Players.Clear(); return Task.CompletedTask; }
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Players.Count);
    }

    private sealed class InMemoryMatchRepository : IFootballMatchRepository
    {
        public List<MatchEntity> Matches { get; } = [];

        public Task<int> GetNextIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(Matches.Count == 0 ? 1 : Matches.Max(m => m.Id) + 1);
        public Task<MatchEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Matches.FirstOrDefault(m => m.Id == id));
        public Task AddAsync(MatchEntity match, CancellationToken cancellationToken = default) { Matches.Add(match); return Task.CompletedTask; }
        public Task UpdateAsync(MatchEntity match, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(int id, CancellationToken cancellationToken = default) { Matches.RemoveAll(m => m.Id == id); return Task.CompletedTask; }
        public Task AddRangeAsync(IReadOnlyCollection<MatchEntity> matches, CancellationToken cancellationToken = default) { Matches.AddRange(matches); return Task.CompletedTask; }
        public Task DeleteAllAsync(CancellationToken cancellationToken = default) { Matches.Clear(); return Task.CompletedTask; }
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Matches.Count);
    }

    private sealed class InMemoryTeamRepository : ITeamRepository
    {
        public List<TeamEntity> Teams { get; } = [];

        public Task<int> GetNextIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(Teams.Count == 0 ? 1 : Teams.Max(t => t.Id) + 1);
        public Task<TeamEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Teams.FirstOrDefault(t => t.Id == id));
        public Task AddAsync(TeamEntity team, CancellationToken cancellationToken = default) { Teams.Add(team); return Task.CompletedTask; }
        public Task AddRangeAsync(IReadOnlyCollection<TeamEntity> teams, CancellationToken cancellationToken = default) { Teams.AddRange(teams); return Task.CompletedTask; }
        public Task DeleteAllAsync(CancellationToken cancellationToken = default) { Teams.Clear(); return Task.CompletedTask; }
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Teams.Count);
    }

    private sealed class InMemoryLeagueRepository : ILeagueRepository
    {
        public List<LeagueEntity> Leagues { get; } = [];

        public Task<int> GetNextIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(Leagues.Count == 0 ? 1 : Leagues.Max(l => l.Id) + 1);
        public Task<LeagueEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Leagues.FirstOrDefault(l => l.Id == id));
        public Task AddAsync(LeagueEntity league, CancellationToken cancellationToken = default) { Leagues.Add(league); return Task.CompletedTask; }
        public Task AddRangeAsync(IReadOnlyCollection<LeagueEntity> leagues, CancellationToken cancellationToken = default) { Leagues.AddRange(leagues); return Task.CompletedTask; }
        public Task DeleteAllAsync(CancellationToken cancellationToken = default) { Leagues.Clear(); return Task.CompletedTask; }
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Leagues.Count);
    }
}
