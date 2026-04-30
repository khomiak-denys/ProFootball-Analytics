using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure.Persistence;
using ProFootball.Infrastructure.Persistence.Repositories;
using Xunit;

namespace ProFootball.Infrastructure.Tests.Features.Persistence;

public class EfTeamAndLeagueRepositoryTests
{
    [Fact]
    public async Task TeamRepository_AddAsync_ShouldPersistTeam()
    {
        await using var host = await SqliteTestHost.CreateAsync();
        var repository = new EfTeamRepository(host.Factory);

        var nextId = await repository.GetNextIdAsync();
        await repository.AddAsync(new Team(nextId, "FC Test", "FCT"));

        await using var dbContext = await host.Factory.CreateDbContextAsync();
        var team = await dbContext.Teams.AsNoTracking().SingleAsync();
        Assert.Equal("FC Test", team.LongName);
        Assert.Equal("FCT", team.ShortName);
    }

    [Fact]
    public async Task LeagueRepository_AddAsync_ShouldPersistLeague()
    {
        await using var host = await SqliteTestHost.CreateAsync();
        var repository = new EfLeagueRepository(host.Factory);

        var nextId = await repository.GetNextIdAsync();
        await repository.AddAsync(new League(nextId, "Ukraine", "Premier League", 16, "Top level"));

        await using var dbContext = await host.Factory.CreateDbContextAsync();
        var league = await dbContext.Leagues.AsNoTracking().SingleAsync();
        Assert.Equal("Ukraine", league.CountryName);
        Assert.Equal("Premier League", league.Name);
        Assert.Equal(16, league.MaxTeams);
        Assert.Equal("Top level", league.Description);
    }

    private sealed class SqliteTestHost : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private SqliteTestHost(SqliteConnection connection, DbContextOptions<ProFootballDbContext> options)
        {
            _connection = connection;
            Factory = new TestDbContextFactory(options);
        }

        public IDbContextFactory<ProFootballDbContext> Factory { get; }

        public static async Task<SqliteTestHost> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ProFootballDbContext>()
                .UseSqlite(connection)
                .Options;

            await using (var dbContext = new ProFootballDbContext(options))
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            return new SqliteTestHost(connection, options);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<ProFootballDbContext> options) : IDbContextFactory<ProFootballDbContext>
    {
        public ProFootballDbContext CreateDbContext() => new(options);

        public ValueTask<ProFootballDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(CreateDbContext());
    }
}
