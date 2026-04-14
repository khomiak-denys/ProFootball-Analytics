using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Importing;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure.Persistence;

namespace ProFootball.Infrastructure.Importing;

public sealed class DataImportService(
    ProFootballDbContext dbContext,
    ILogger<DataImportService> logger) : IDataImportService
{
    public async Task<DataImportResult> ImportAsync(
        DataImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SqlitePath);

        if (!File.Exists(request.SqlitePath))
        {
            throw new FileNotFoundException("SQLite source file was not found.", request.SqlitePath);
        }

        var batchSize = request.BatchSize < 100 ? 100 : request.BatchSize;
        var stopwatch = Stopwatch.StartNew();

        await dbContext.Database.MigrateAsync(cancellationToken);

        await using var importTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await ClearExistingDataAsync(cancellationToken);

            await using var sqliteConnection = new SqliteConnection($"Data Source={request.SqlitePath};Mode=ReadOnly;Cache=Shared");
            await sqliteConnection.OpenAsync(cancellationToken);

            var skipped = 0;
            var countries = await ImportCountriesAsync(sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var leagues = await ImportLeaguesAsync(sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var teams = await ImportTeamsAsync(sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var players = await ImportPlayersAsync(sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var matches = await ImportMatchesAsync(sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var teamAttributes = await ImportTeamAttributesAsync(sqliteConnection, batchSize, value => skipped += value, cancellationToken);
            var playerAttributes = await ImportPlayerAttributesAsync(sqliteConnection, batchSize, value => skipped += value, cancellationToken);

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
        catch
        {
            await importTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ClearExistingDataAsync(CancellationToken cancellationToken)
    {
        await dbContext.PlayerAttributes.ExecuteDeleteAsync(cancellationToken);
        await dbContext.TeamAttributes.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Matches.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Players.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Teams.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Leagues.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Countries.ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<int> ImportCountriesAsync(
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
            var id = GetRequiredInt(reader, 0);
            var name = GetRequiredString(reader, 1);
            if (!id.HasValue || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            batch.Add(new Country(id.Value, name));
            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported countries: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportLeaguesAsync(
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
            var id = GetRequiredInt(reader, 0);
            var countryId = GetRequiredInt(reader, 1);
            var name = GetRequiredString(reader, 2);
            if (!id.HasValue || !countryId.HasValue || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            batch.Add(new League(id.Value, countryId.Value, name));
            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported leagues: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportTeamsAsync(
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
            var id = GetRequiredInt(reader, 0);
            var teamApiId = GetRequiredInt(reader, 1);
            var teamFifaApiId = GetOptionalInt(reader, 2);
            var longName = GetRequiredString(reader, 3);
            var shortName = GetOptionalString(reader, 4);
            if (!id.HasValue || !teamApiId.HasValue || string.IsNullOrWhiteSpace(longName))
            {
                skipped++;
                continue;
            }

            batch.Add(new Team(id.Value, teamApiId.Value, teamFifaApiId, longName, shortName));
            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported teams: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportPlayersAsync(
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
            var id = GetRequiredInt(reader, 0);
            var playerApiId = GetRequiredInt(reader, 1);
            var playerFifaApiId = GetOptionalInt(reader, 2);
            var name = GetRequiredString(reader, 3);
            var birthday = GetOptionalDateTime(reader, 4);
            var height = GetOptionalInt(reader, 5);
            var weight = GetOptionalInt(reader, 6);
            if (!id.HasValue || !playerApiId.HasValue || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            batch.Add(new Player(id.Value, playerApiId.Value, playerFifaApiId, name, birthday, height, weight));
            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported players: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportMatchesAsync(
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
            var id = GetRequiredInt(reader, 0);
            var countryId = GetRequiredInt(reader, 1);
            var leagueId = GetRequiredInt(reader, 2);
            var season = GetRequiredString(reader, 3);
            var date = GetOptionalDateTime(reader, 4);
            var matchApiId = GetRequiredInt(reader, 5);
            var homeTeamApiId = GetRequiredInt(reader, 6);
            var awayTeamApiId = GetRequiredInt(reader, 7);
            var homeTeamGoal = GetOptionalInt(reader, 8);
            var awayTeamGoal = GetOptionalInt(reader, 9);

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
                imported += await PersistBatchAsync(batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported matches: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportTeamAttributesAsync(
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
            var id = GetRequiredInt(reader, 0);
            var teamFifaApiId = GetOptionalInt(reader, 1);
            var teamApiId = GetRequiredInt(reader, 2);
            var date = GetOptionalDateTime(reader, 3);
            var buildUpPlaySpeed = GetOptionalInt(reader, 4);
            var buildUpPlayPassing = GetOptionalInt(reader, 5);
            var chanceCreationPassing = GetOptionalInt(reader, 6);
            var defencePressure = GetOptionalInt(reader, 7);

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
                imported += await PersistBatchAsync(batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported team attributes: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> ImportPlayerAttributesAsync(
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
            var id = GetRequiredInt(reader, 0);
            var playerFifaApiId = GetOptionalInt(reader, 1);
            var playerApiId = GetRequiredInt(reader, 2);
            var date = GetOptionalDateTime(reader, 3);
            var overallRating = GetOptionalInt(reader, 4);
            var potential = GetOptionalInt(reader, 5);
            var preferredFoot = GetOptionalString(reader, 6);
            var attackingWorkRate = GetOptionalString(reader, 7);
            var defensiveWorkRate = GetOptionalString(reader, 8);

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
                imported += await PersistBatchAsync(batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(batch, cancellationToken);
        addSkipped(skipped);

        logger.LogInformation("Imported player attributes: {Imported}, skipped: {Skipped}", imported, skipped);
        return imported;
    }

    private async Task<int> PersistBatchAsync<TEntity>(
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

    private static int? GetRequiredInt(SqliteDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        return reader.GetValue(index) switch
        {
            long value => checked((int)value),
            int value => value,
            double value => checked((int)value),
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static int? GetOptionalInt(SqliteDataReader reader, int index) => GetRequiredInt(reader, index);

    private static string? GetRequiredString(SqliteDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        return reader.GetString(index).Trim();
    }

    private static string? GetOptionalString(SqliteDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        return reader.GetString(index).Trim();
    }

    private static DateTime? GetOptionalDateTime(SqliteDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index);
        if (value is DateTime dateTimeValue)
        {
            return dateTimeValue;
        }

        if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        return null;
    }
}
