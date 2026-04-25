using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ProFootball.Application.Importing.Commands;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure.Importing;
using ProFootball.Infrastructure.Persistence;
using ProFootball.Infrastructure.Persistence.Repositories;
using Xunit;

namespace ProFootball.Infrastructure.Tests.Features.Importing;

public class DataImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ShouldThrow_WhenSqlitePathIsWhitespace()
    {
        await using var destinationConnection = new SqliteConnection("Data Source=:memory:");
        await destinationConnection.OpenAsync();

        var options = new DbContextOptionsBuilder<ProFootballDbContext>()
            .UseSqlite(destinationConnection)
            .Options;

        var service = CreateService(options);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            service.HandleAsync(new ImportDataCommand("   ", BatchSize: 100)));
    }

    [Fact]
    public async Task ImportAsync_ShouldThrow_WhenSqliteFileDoesNotExist()
    {
        await using var destinationConnection = new SqliteConnection("Data Source=:memory:");
        await destinationConnection.OpenAsync();

        var options = new DbContextOptionsBuilder<ProFootballDbContext>()
            .UseSqlite(destinationConnection)
            .Options;

        var service = CreateService(options);
        var missingFilePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.sqlite");

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.HandleAsync(new ImportDataCommand(missingFilePath, BatchSize: 100)));
    }

    [Fact]
    public async Task ImportAsync_ShouldImportValidRowsAndCountSkipped()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"profootball-source-{Guid.NewGuid():N}.sqlite");

        try
        {
            await CreateSourceSqliteAsync(sourcePath);

            await using var destinationConnection = new SqliteConnection("Data Source=:memory:");
            await destinationConnection.OpenAsync();

            var options = new DbContextOptionsBuilder<ProFootballDbContext>()
                .UseSqlite(destinationConnection)
                .Options;

            await using var destinationContext = new ProFootballDbContext(options);
            var service = CreateService(options);

            var result = await service.HandleAsync(new ImportDataCommand(sourcePath, BatchSize: 2));

            Assert.Equal(1, result.CountriesResolved);
            Assert.Equal(1, result.LeaguesImported);
            Assert.Equal(1, result.TeamsImported);
            Assert.Equal(1, result.PlayersImported);
            Assert.Equal(1, result.MatchesImported);
            Assert.Equal(1, result.TeamAttributesImported);
            Assert.Equal(1, result.PlayerAttributesImported);
            Assert.Equal(7, result.SkippedRows);

            Assert.Equal(1, await destinationContext.Leagues.CountAsync());
            Assert.Equal(1, await destinationContext.Teams.CountAsync());
            Assert.Equal(1, await destinationContext.Players.CountAsync());
            Assert.Equal(1, await destinationContext.Matches.CountAsync());
            Assert.Equal(1, await destinationContext.TeamAttributes.CountAsync());
            Assert.Equal(1, await destinationContext.PlayerAttributes.CountAsync());
        }
        finally
        {
            await DeleteFileWithRetryAsync(sourcePath);
        }
    }

    [Fact]
    public async Task ImportAsync_ShouldThrow_WhenBatchSizeIsNotPositive()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"profootball-source-{Guid.NewGuid():N}.sqlite");

        try
        {
            await CreateSourceSqliteAsync(sourcePath);

            await using var destinationConnection = new SqliteConnection("Data Source=:memory:");
            await destinationConnection.OpenAsync();

            var options = new DbContextOptionsBuilder<ProFootballDbContext>()
                .UseSqlite(destinationConnection)
                .Options;

            var service = CreateService(options);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.HandleAsync(new ImportDataCommand(sourcePath, BatchSize: 0)));
        }
        finally
        {
            await DeleteFileWithRetryAsync(sourcePath);
        }
    }

    [Fact]
    public async Task ImportAsync_ShouldSupportSqlitePathWithSemicolon()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"profootball;source-{Guid.NewGuid():N}.sqlite");

        try
        {
            await CreateSourceSqliteAsync(sourcePath);

            await using var destinationConnection = new SqliteConnection("Data Source=:memory:");
            await destinationConnection.OpenAsync();

            var options = new DbContextOptionsBuilder<ProFootballDbContext>()
                .UseSqlite(destinationConnection)
                .Options;

            var service = CreateService(options);
            var result = await service.HandleAsync(new ImportDataCommand(sourcePath, BatchSize: 2));

            Assert.Equal(1, result.CountriesResolved);
            Assert.Equal(1, result.LeaguesImported);
            Assert.Equal(1, result.TeamsImported);
            Assert.Equal(1, result.PlayersImported);
            Assert.Equal(1, result.MatchesImported);
            Assert.Equal(1, result.TeamAttributesImported);
            Assert.Equal(1, result.PlayerAttributesImported);
        }
        finally
        {
            await DeleteFileWithRetryAsync(sourcePath);
        }
    }

    [Fact]
    public async Task ImportAsync_ShouldRollbackDestinationData_WhenImportFailsMidTransaction()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"profootball-failing-source-{Guid.NewGuid():N}.sqlite");

        try
        {
            await CreateFailingSourceSqliteAsync(sourcePath);

            await using var destinationConnection = new SqliteConnection("Data Source=:memory:");
            await destinationConnection.OpenAsync();

            var options = new DbContextOptionsBuilder<ProFootballDbContext>()
                .UseSqlite(destinationConnection)
                .Options;

            await using var destinationContext = new ProFootballDbContext(options);
            await destinationContext.Database.EnsureCreatedAsync();
            destinationContext.Leagues.Add(new League(77, "Baseline Country", "Baseline League"));
            await destinationContext.SaveChangesAsync();

            var service = CreateService(options);

            await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
                service.HandleAsync(new ImportDataCommand(sourcePath, BatchSize: 1)));

            var leagues = await destinationContext.Leagues
                .AsNoTracking()
                .OrderBy(league => league.Id)
                .ToListAsync();

            Assert.Single(leagues);
            Assert.Equal(77, leagues[0].Id);
            Assert.Equal("Baseline League", leagues[0].Name);
            Assert.Equal("Baseline Country", leagues[0].CountryName);
        }
        finally
        {
            await DeleteFileWithRetryAsync(sourcePath);
        }
    }

    private static async Task CreateSourceSqliteAsync(string sourcePath)
    {
        var sourceConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Pooling = false,
        }.ToString();

        await using var sourceConnection = new SqliteConnection(sourceConnectionString);
        await sourceConnection.OpenAsync();

        var statements = new[]
        {
            "CREATE TABLE Country (id INTEGER, name TEXT);",
            "CREATE TABLE League (id INTEGER, country_id INTEGER, name TEXT);",
            "CREATE TABLE Team (id INTEGER, team_api_id INTEGER, team_fifa_api_id INTEGER, team_long_name TEXT, team_short_name TEXT);",
            "CREATE TABLE Player (id INTEGER, player_api_id INTEGER, player_fifa_api_id INTEGER, player_name TEXT, birthday TEXT, height INTEGER, weight INTEGER);",
            "CREATE TABLE [Match] (id INTEGER, country_id INTEGER, league_id INTEGER, season TEXT, date TEXT, match_api_id INTEGER, home_team_api_id INTEGER, away_team_api_id INTEGER, home_team_goal INTEGER, away_team_goal INTEGER);",
            "CREATE TABLE Team_Attributes (id INTEGER, team_fifa_api_id INTEGER, team_api_id INTEGER, date TEXT, buildUpPlaySpeed INTEGER, buildUpPlayPassing INTEGER, chanceCreationPassing INTEGER, defencePressure INTEGER);",
            "CREATE TABLE Player_Attributes (id INTEGER, player_fifa_api_id INTEGER, player_api_id INTEGER, date TEXT, overall_rating INTEGER, potential INTEGER, preferred_foot TEXT, attacking_work_rate TEXT, defensive_work_rate TEXT);",

            "INSERT INTO Country VALUES (1, 'Ukraine');",
            "INSERT INTO Country VALUES (2, NULL);",

            "INSERT INTO League VALUES (1, 1, 'Premier League');",
            "INSERT INTO League VALUES (2, NULL, 'Broken League');",

            "INSERT INTO Team VALUES (1, 100, 1000, 'Dynamo Kyiv', 'DYK');",
            "INSERT INTO Team VALUES (2, NULL, 1001, 'Broken Team', 'BRK');",

            "INSERT INTO Player VALUES (1, 200, 2000, 'Andriy Yarmolenko', '1989-10-23 00:00:00', 189, 81);",
            "INSERT INTO Player VALUES (2, 201, 2001, NULL, '1990-01-01 00:00:00', 180, 75);",

            "INSERT INTO [Match] VALUES (1, 1, 1, '2015/2016', '2015-08-10 00:00:00', 300, 100, 100, 2, 1);",
            "INSERT INTO [Match] VALUES (2, 1, 1, '2015/2016', NULL, 301, 100, 100, 1, 0);",

            "INSERT INTO Team_Attributes VALUES (1, 1000, 100, '2015-08-10 00:00:00', 55, 60, 62, 48);",
            "INSERT INTO Team_Attributes VALUES (2, 1000, 100, NULL, 55, 60, 62, 48);",

            "INSERT INTO Player_Attributes VALUES (1, 2000, 200, '2015-08-10 00:00:00', 78, 82, 'right', 'high', 'medium');",
            "INSERT INTO Player_Attributes VALUES (2, 2000, 200, NULL, 78, 82, 'right', 'high', 'medium');",
        };

        foreach (var statement in statements)
        {
            await using var command = sourceConnection.CreateCommand();
            command.CommandText = statement;
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task CreateFailingSourceSqliteAsync(string sourcePath)
    {
        var sourceConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Pooling = false,
        }.ToString();

        await using var sourceConnection = new SqliteConnection(sourceConnectionString);
        await sourceConnection.OpenAsync();

        var statements = new[]
        {
            "CREATE TABLE Country (id INTEGER, name TEXT);",
            "CREATE TABLE League (id INTEGER, country_id INTEGER, name TEXT);",
            "CREATE TABLE Team (id INTEGER, team_api_id INTEGER, team_fifa_api_id INTEGER, team_long_name TEXT, team_short_name TEXT);",
            "CREATE TABLE Player (id INTEGER, player_api_id INTEGER, player_fifa_api_id INTEGER, player_name TEXT, birthday TEXT, height INTEGER, weight INTEGER);",
            "CREATE TABLE [Match] (id INTEGER, country_id INTEGER, league_id INTEGER, season TEXT, date TEXT, match_api_id INTEGER, home_team_api_id INTEGER, away_team_api_id INTEGER, home_team_goal INTEGER, away_team_goal INTEGER);",
            "CREATE TABLE Team_Attributes (id INTEGER, team_fifa_api_id INTEGER, team_api_id INTEGER, date TEXT, buildUpPlaySpeed INTEGER, buildUpPlayPassing INTEGER, chanceCreationPassing INTEGER, defencePressure INTEGER);",
            "CREATE TABLE Player_Attributes (id INTEGER, player_fifa_api_id INTEGER, player_api_id INTEGER, date TEXT, overall_rating INTEGER, potential INTEGER, preferred_foot TEXT, attacking_work_rate TEXT, defensive_work_rate TEXT);",
            "INSERT INTO Country VALUES (1, 'Ukraine');",
            "INSERT INTO League VALUES (1, 1, 'League One');",
            "INSERT INTO League VALUES (1, 1, 'League Duplicate');",
        };

        foreach (var statement in statements)
        {
            await using var command = sourceConnection.CreateCommand();
            command.CommandText = statement;
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task DeleteFileWithRetryAsync(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        IOException? lastException = null;
        const int maxAttempts = 20;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException exception) when (attempt < maxAttempts - 1)
            {
                lastException = exception;
                await Task.Delay(100);
            }
            catch (IOException exception)
            {
                lastException = exception;
            }
        }

        throw new IOException(
            $"[DataImportServiceTests] Failed to delete temporary SQLite file '{path}' after retries. " +
            $"Last error: {lastException?.Message}",
            lastException);
    }

    private static DataImportService CreateService(DbContextOptions<ProFootballDbContext> options)
    {
        var dbContext = new ProFootballDbContext(options);
        return new DataImportService(
            dbContext,
            new EfLeagueRepository(dbContext),
            new EfTeamRepository(dbContext),
            new EfPlayerRepository(dbContext),
            new EfFootballMatchRepository(dbContext),
            new EfTeamAttributeRepository(dbContext),
            new EfPlayerAttributeRepository(dbContext),
            NullLogger<DataImportService>.Instance);
    }
}

