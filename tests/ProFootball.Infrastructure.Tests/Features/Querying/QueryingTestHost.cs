using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Tests.Features.Querying;

internal sealed class QueryingTestHost : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ProFootballDbContext> _options;

    private QueryingTestHost(SqliteConnection connection, DbContextOptions<ProFootballDbContext> options)
    {
        _connection = connection;
        _options = options;
        DbContextFactory = new TestDbContextFactory(_options);
    }

    public IDbContextFactory<ProFootballDbContext> DbContextFactory { get; }

    public static async Task<QueryingTestHost> CreateAsync()
    {
        SqliteConnection? connection = null;
        try
        {
            connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ProFootballDbContext>()
                .UseSqlite(connection)
                .Options;

            await using (var dbContext = new ProFootballDbContext(options))
            {
                await dbContext.Database.EnsureCreatedAsync();
                await SeedAsync(dbContext);
            }

            return new QueryingTestHost(connection, options);
        }
        catch
        {
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private static async Task SeedAsync(ProFootballDbContext dbContext)
    {
        dbContext.Leagues.AddRange(
            new League(10, "England", "Premier League"),
            new League(11, "England", "Championship"),
            new League(20, "Spain", "La Liga"));

        dbContext.Teams.AddRange(
            new Team(1, "Arsenal", "ARS"),
            new Team(2, "Barcelona", "BAR"),
            new Team(3, "Chelsea", "CHE"),
            new Team(4, "Wolves", "WOL"));

        dbContext.Players.AddRange(
            new Player(1, "Kevin De Bruyne", new DateTime(1991, 6, 28, 0, 0, 0, DateTimeKind.Utc), 181, 70),
            new Player(2, "Erling Haaland", new DateTime(2000, 7, 21, 0, 0, 0, DateTimeKind.Utc), 194, 88),
            new Player(3, "Unrated Player", null, null, null));

        dbContext.PlayerAttributes.AddRange(
            new PlayerAttribute(1, 1, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 90, 91, "right", "high", "medium"),
            new PlayerAttribute(2, 1, new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), 92, 93, "right", "high", "medium"),
            new PlayerAttribute(3, 2, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 89, 94, "left", "high", "medium"),
            new PlayerAttribute(4, 2, new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), 91, 95, "left", "high", "medium"),
            new PlayerAttribute(5, 2, new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), 90, 95, "left", "high", "medium"));

        dbContext.TeamAttributes.AddRange(
            new TeamAttribute(1, 1, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 70, 72, 73, 74),
            new TeamAttribute(2, 1, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 71, 73, 74, 75),
            new TeamAttribute(3, 2, new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc), 68, 70, 72, 71));

        dbContext.Matches.AddRange(
            new FootballMatch(1, "England", 10, "2024/2025", new DateTime(2024, 8, 10, 0, 0, 0, DateTimeKind.Utc), 1, 3, 2, 1),
            new FootballMatch(2, "England", 11, "2024/2025", new DateTime(2024, 8, 11, 0, 0, 0, DateTimeKind.Utc), 3, 1, 1, 1),
            new FootballMatch(3, "Spain", 20, "2023/2024", new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc), 2, 4, 3, 0));

        await dbContext.SaveChangesAsync();
    }

    private sealed class TestDbContextFactory(DbContextOptions<ProFootballDbContext> options) : IDbContextFactory<ProFootballDbContext>
    {
        public ProFootballDbContext CreateDbContext() => new(options);
    }
}
