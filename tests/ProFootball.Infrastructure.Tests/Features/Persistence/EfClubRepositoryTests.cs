using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure.Persistence;
using ProFootball.Infrastructure.Persistence.Repositories;
using Xunit;

namespace ProFootball.Infrastructure.Tests.Features.Persistence;

public class EfClubRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnClubsOrderedByName()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ProFootballDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new ProFootballDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.FootballClubs.AddRange(
                new FootballClub("Chelsea", 1905),
                new FootballClub("Arsenal", 1886),
                new FootballClub("Barcelona", 1899));
            await setupContext.SaveChangesAsync();
        }

        var repository = new EfClubRepository(new TestDbContextFactory(options));

        var clubs = await repository.GetAllAsync();

        Assert.Equal(3, clubs.Count);
        Assert.Equal(new[] { "Arsenal", "Barcelona", "Chelsea" }, clubs.Select(club => club.Name));
    }

    private sealed class TestDbContextFactory(DbContextOptions<ProFootballDbContext> options) : IDbContextFactory<ProFootballDbContext>
    {
        public ProFootballDbContext CreateDbContext() => new(options);
    }
}
