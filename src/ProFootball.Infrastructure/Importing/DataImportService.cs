using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Importing.Commands;
using ProFootball.Application.Importing.Dtos;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Importing;

public sealed class DataImportService(
    IDbContextFactory<ProFootballDbContext> dbContextFactory,
    ILogger<DataImportService> logger) : ICommandHandler<ImportDataCommand, DataImportResult>
{
    public async Task<DataImportResult> HandleAsync(
        ImportDataCommand request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SqlitePath);

        if (!File.Exists(request.SqlitePath))
        {
            throw new FileNotFoundException("SQLite source file was not found.", request.SqlitePath);
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(request.BatchSize, 1);
        var batchSize = request.BatchSize;
        var stopwatch = Stopwatch.StartNew();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        await PrepareDatabaseAsync(dbContext, cancellationToken);

        await using var importTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await ClearExistingDataAsync(dbContext, cancellationToken);

            var sqliteConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = request.SqlitePath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,
                Pooling = false,
            }.ToString();

            await using var sqliteConnection = new SqliteConnection(sqliteConnectionString);
            await sqliteConnection.OpenAsync(cancellationToken);

            var skipped = 0;
            var countries = await ImportCountriesAsync(dbContext, sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var leagues = await ImportLeaguesAsync(dbContext, sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var teams = await ImportTeamsAsync(dbContext, sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var players = await ImportPlayersAsync(dbContext, sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var matches = await ImportMatchesAsync(dbContext, sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var teamAttributes = await ImportTeamAttributesAsync(dbContext, sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var playerAttributes = await ImportPlayerAttributesAsync(dbContext, sqliteConnection, batchSize, value => skipped += value, cancellationToken);

            await importTransaction.CommitAsync(cancellationToken);

            stopwatch.Stop();

            return new DataImportResult(
                countries,
                leagues,
                teams,
                players,
                matches,
                teamAttributes,
                playerAttributes,
                skipped,
                stopwatch.Elapsed);
        }
        catch (Exception)
        {
            try
            {
                await importTransaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                logger.LogWarning(
                    rollbackException,
                    "Rollback failed after import error for source '{SqlitePath}'.",
                    request.SqlitePath);
            }

            throw;
        }
    }

    private static async Task ClearExistingDataAsync(ProFootballDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.PlayerAttributes.ExecuteDeleteAsync(cancellationToken);
        await dbContext.TeamAttributes.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Matches.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Players.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Teams.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Leagues.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Countries.ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task PrepareDatabaseAsync(ProFootballDbContext dbContext, CancellationToken cancellationToken)
    {
        var providerName = dbContext.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private async Task<int> ImportCountriesAsync(
        ProFootballDbContext dbContext,
        SqliteConnection sqliteConnection,
        int batchSize,
        Action<int> addSkipped,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, name FROM Country";
        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = sql;

        var imported = 0;
        var skipped = 0;
        var batch = new List<Country>(batchSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = SqliteValueParser.ReadInt32(reader, 0);
            var name = SqliteValueParser.ReadString(reader, 1);
            if (!id.HasValue || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            batch.Add(new Country(id.Value, name));
            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported countries: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportLeaguesAsync(
        ProFootballDbContext dbContext,
        SqliteConnection sqliteConnection,
        int batchSize,
        Action<int> addSkipped,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, country_id, name FROM League";
        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = sql;

        var imported = 0;
        var skipped = 0;
        var batch = new List<League>(batchSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = SqliteValueParser.ReadInt32(reader, 0);
            var countryId = SqliteValueParser.ReadInt32(reader, 1);
            var name = SqliteValueParser.ReadString(reader, 2);
            if (!id.HasValue || !countryId.HasValue || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            batch.Add(new League(id.Value, countryId.Value, name));
            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported leagues: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportTeamsAsync(
        ProFootballDbContext dbContext,
        SqliteConnection sqliteConnection,
        int batchSize,
        Action<int> addSkipped,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, team_api_id, team_fifa_api_id, team_long_name, team_short_name FROM Team";
        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = sql;

        var imported = 0;
        var skipped = 0;
        var batch = new List<Team>(batchSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = SqliteValueParser.ReadInt32(reader, 0);
            var teamApiId = SqliteValueParser.ReadInt32(reader, 1);
            var teamFifaApiId = SqliteValueParser.ReadInt32(reader, 2);
            var longName = SqliteValueParser.ReadString(reader, 3);
            var shortName = SqliteValueParser.ReadString(reader, 4);
            if (!id.HasValue || !teamApiId.HasValue || string.IsNullOrWhiteSpace(longName))
            {
                skipped++;
                continue;
            }

            batch.Add(new Team(id.Value, teamApiId.Value, teamFifaApiId, longName, shortName));
            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported teams: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportPlayersAsync(
        ProFootballDbContext dbContext,
        SqliteConnection sqliteConnection,
        int batchSize,
        Action<int> addSkipped,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, player_api_id, player_fifa_api_id, player_name, birthday, height, weight FROM Player";
        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = sql;

        var imported = 0;
        var skipped = 0;
        var batch = new List<Player>(batchSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = SqliteValueParser.ReadInt32(reader, 0);
            var playerApiId = SqliteValueParser.ReadInt32(reader, 1);
            var playerFifaApiId = SqliteValueParser.ReadInt32(reader, 2);
            var name = SqliteValueParser.ReadString(reader, 3);
            var birthday = SqliteValueParser.ReadDateTime(reader, 4);
            var height = SqliteValueParser.ReadInt32(reader, 5);
            var weight = SqliteValueParser.ReadInt32(reader, 6);
            if (!id.HasValue || !playerApiId.HasValue || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            batch.Add(new Player(id.Value, playerApiId.Value, playerFifaApiId, name, birthday, height, weight));
            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported players: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportMatchesAsync(
        ProFootballDbContext dbContext,
        SqliteConnection sqliteConnection,
        int batchSize,
        Action<int> addSkipped,
        CancellationToken cancellationToken)
    {
        const string sql = @"SELECT id, country_id, league_id, season, date, match_api_id, home_team_api_id, away_team_api_id, home_team_goal, away_team_goal
                             FROM [Match]";

        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = sql;

        var imported = 0;
        var skipped = 0;
        var batch = new List<FootballMatch>(batchSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = SqliteValueParser.ReadInt32(reader, 0);
            var countryId = SqliteValueParser.ReadInt32(reader, 1);
            var leagueId = SqliteValueParser.ReadInt32(reader, 2);
            var season = SqliteValueParser.ReadString(reader, 3);
            var date = SqliteValueParser.ReadDateTime(reader, 4);
            var matchApiId = SqliteValueParser.ReadInt32(reader, 5);
            var homeTeamApiId = SqliteValueParser.ReadInt32(reader, 6);
            var awayTeamApiId = SqliteValueParser.ReadInt32(reader, 7);
            var homeTeamGoal = SqliteValueParser.ReadInt32(reader, 8);
            var awayTeamGoal = SqliteValueParser.ReadInt32(reader, 9);

            if (!id.HasValue
                || !countryId.HasValue
                || !leagueId.HasValue
                || !date.HasValue
                || !matchApiId.HasValue
                || !homeTeamApiId.HasValue
                || !awayTeamApiId.HasValue
                || string.IsNullOrWhiteSpace(season))
            {
                skipped++;
                continue;
            }

            batch.Add(new FootballMatch(
                id.Value,
                countryId.Value,
                leagueId.Value,
                season,
                date.Value,
                matchApiId.Value,
                homeTeamApiId.Value,
                awayTeamApiId.Value,
                homeTeamGoal,
                awayTeamGoal));

            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported matches: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportTeamAttributesAsync(
        ProFootballDbContext dbContext,
        SqliteConnection sqliteConnection,
        int batchSize,
        Action<int> addSkipped,
        CancellationToken cancellationToken)
    {
        const string sql = @"SELECT id, team_fifa_api_id, team_api_id, date, buildUpPlaySpeed, buildUpPlayPassing, chanceCreationPassing, defencePressure
                             FROM Team_Attributes";

        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = sql;

        var imported = 0;
        var skipped = 0;
        var batch = new List<TeamAttribute>(batchSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = SqliteValueParser.ReadInt32(reader, 0);
            var teamFifaApiId = SqliteValueParser.ReadInt32(reader, 1);
            var teamApiId = SqliteValueParser.ReadInt32(reader, 2);
            var date = SqliteValueParser.ReadDateTime(reader, 3);
            var buildUpPlaySpeed = SqliteValueParser.ReadInt32(reader, 4);
            var buildUpPlayPassing = SqliteValueParser.ReadInt32(reader, 5);
            var chanceCreationPassing = SqliteValueParser.ReadInt32(reader, 6);
            var defencePressure = SqliteValueParser.ReadInt32(reader, 7);

            if (!id.HasValue || !teamApiId.HasValue || !date.HasValue)
            {
                skipped++;
                continue;
            }

            batch.Add(new TeamAttribute(
                id.Value,
                teamApiId.Value,
                teamFifaApiId,
                date.Value,
                buildUpPlaySpeed,
                buildUpPlayPassing,
                chanceCreationPassing,
                defencePressure));

            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported team attributes: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportPlayerAttributesAsync(
        ProFootballDbContext dbContext,
        SqliteConnection sqliteConnection,
        int batchSize,
        Action<int> addSkipped,
        CancellationToken cancellationToken)
    {
        const string sql = @"SELECT id, player_fifa_api_id, player_api_id, date, overall_rating, potential, preferred_foot, attacking_work_rate, defensive_work_rate
                             FROM Player_Attributes";

        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = sql;

        var imported = 0;
        var skipped = 0;
        var batch = new List<PlayerAttribute>(batchSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = SqliteValueParser.ReadInt32(reader, 0);
            var playerFifaApiId = SqliteValueParser.ReadInt32(reader, 1);
            var playerApiId = SqliteValueParser.ReadInt32(reader, 2);
            var date = SqliteValueParser.ReadDateTime(reader, 3);
            var overallRating = SqliteValueParser.ReadInt32(reader, 4);
            var potential = SqliteValueParser.ReadInt32(reader, 5);
            var preferredFoot = SqliteValueParser.ReadString(reader, 6);
            var attackingWorkRate = SqliteValueParser.ReadString(reader, 7);
            var defensiveWorkRate = SqliteValueParser.ReadString(reader, 8);

            if (!id.HasValue || !playerApiId.HasValue || !date.HasValue)
            {
                skipped++;
                continue;
            }

            batch.Add(new PlayerAttribute(
                id.Value,
                playerApiId.Value,
                playerFifaApiId,
                date.Value,
                overallRating,
                potential,
                preferredFoot,
                attackingWorkRate,
                defensiveWorkRate));

            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported player attributes: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private static async Task<int> PersistBatchAsync<TEntity>(
        ProFootballDbContext dbContext,
        List<TEntity> batch,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (batch.Count == 0)
        {
            return 0;
        }

        dbContext.Set<TEntity>().AddRange(batch);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();

        var persisted = batch.Count;
        batch.Clear();
        return persisted;
    }

}

