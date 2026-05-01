using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
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
    private const string GeneratorVersion = "v1";
    private static readonly string[] EventTypes = ["goal", "yellow_card", "red_card", "substitution", "shot"];

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
        var profile = NormalizeProfile(request.Profile);
        var mode = NormalizeMode(request.Mode);
        var batchSize = request.BatchSize;
        var stopwatch = Stopwatch.StartNew();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        await PrepareDatabaseAsync(dbContext, cancellationToken);

        await using var importTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (mode == "regenerate")
            {
                await ClearExistingDataAsync(dbContext, cancellationToken);
            }
            var shouldImportBaseData = mode == "regenerate" || !await dbContext.Matches.AnyAsync(cancellationToken);

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
            var countriesResolved = 0;
            var leagues = 0;
            var teams = 0;
            var players = 0;
            var matches = 0;
            var teamAttributes = 0;
            var playerAttributes = 0;

            if (shouldImportBaseData)
            {
                var countryLookup = await ReadCountryLookupAsync(sqliteConnection, value => skipped += value, cancellationToken);
                countriesResolved = countryLookup.Count;
                leagues = await ImportLeaguesAsync(dbContext, sqliteConnection, countryLookup, batchSize, value => skipped += value, cancellationToken);
                var teamsResult = await ImportTeamsAsync(dbContext, sqliteConnection, batchSize, value => skipped += value, cancellationToken);
                teams = teamsResult.Imported;
                var teamIdMap = teamsResult.TeamIdMap;
                var playersResult = await ImportPlayersAsync(dbContext, sqliteConnection, batchSize, value => skipped += value, cancellationToken);
                players = playersResult.Imported;
                var playerIdMap = playersResult.PlayerIdMap;
                matches = await ImportMatchesAsync(dbContext, sqliteConnection, countryLookup, teamIdMap, batchSize, value => skipped += value, cancellationToken);
                teamAttributes = await ImportTeamAttributesAsync(dbContext, sqliteConnection, teamIdMap, batchSize, value => skipped += value, cancellationToken);
                playerAttributes = await ImportPlayerAttributesAsync(dbContext, sqliteConnection, playerIdMap, batchSize, value => skipped += value, cancellationToken);

                await BackfillTeamLeagueLinksAsync(dbContext, cancellationToken);
            }

            var seed = BuildSeed(dbContext.Database.ProviderName ?? "unknown", GeneratorVersion);
            var random = CreateDeterministicRandom(seed);

            var matchEvents = await GenerateMatchEventsAsync(dbContext, profile, mode, random, cancellationToken);
            const int playerMatchStats = 0;
            var teamSeasonStats = await RebuildTeamSeasonStatsAsync(dbContext, cancellationToken);
            var validationErrors = await ValidateGeneratedDataAsync(dbContext, cancellationToken);

            if (validationErrors > 0)
            {
                throw new InvalidOperationException($"Synthetic generation failed with {validationErrors} validation errors.");
            }

            await importTransaction.CommitAsync(cancellationToken);

            stopwatch.Stop();

            return new DataImportResult(
                countriesResolved,
                leagues,
                teams,
                players,
                matches,
                teamAttributes,
                playerAttributes,
                matchEvents,
                playerMatchStats,
                teamSeasonStats,
                validationErrors,
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

    private static string NormalizeProfile(string profile)
    {
        var normalized = profile.Trim().ToLowerInvariant();
        return normalized switch
        {
            "lite" => normalized,
            "realistic" => normalized,
            "stress" => normalized,
            _ => throw new ArgumentOutOfRangeException(nameof(profile), "Profile must be one of: lite, realistic, stress."),
        };
    }

    private static string NormalizeMode(string mode)
    {
        var normalized = mode.Trim().ToLowerInvariant();
        return normalized switch
        {
            "regenerate" => normalized,
            "append" => normalized,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), "Mode must be one of: regenerate, append."),
        };
    }

    private static string BuildSeed(string environmentName, string generatorVersion)
    {
        var raw = $"{environmentName}|{generatorVersion}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static Random CreateDeterministicRandom(string seed)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var value = BitConverter.ToInt32(hash, 0);
        return new Random(value);
    }

    private static async Task ClearExistingDataAsync(ProFootballDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.TeamSeasonStats.ExecuteDeleteAsync(cancellationToken);
        await dbContext.MatchEvents.ExecuteDeleteAsync(cancellationToken);
        await dbContext.PlayerAttributes.ExecuteDeleteAsync(cancellationToken);
        await dbContext.TeamAttributes.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Matches.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Players.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Teams.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Leagues.ExecuteDeleteAsync(cancellationToken);
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

    private async Task<Dictionary<int, string>> ReadCountryLookupAsync(
        SqliteConnection sqliteConnection,
        Action<int> addSkipped,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, name FROM Country";
        await using var command = sqliteConnection.CreateCommand();
        command.CommandText = sql;

        var skipped = 0;
        var countryLookup = new Dictionary<int, string>();

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

            countryLookup[id.Value] = name;
        }

        addSkipped(skipped);
        logger.LogInformation("Resolved countries from SQLite: {Resolved}, skipped: {Skipped}", countryLookup.Count, skipped);
        return countryLookup;
    }

    private async Task<int> ImportLeaguesAsync(
        ProFootballDbContext dbContext,
        SqliteConnection sqliteConnection,
        IReadOnlyDictionary<int, string> countryLookup,
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
            if (!id.HasValue
                || !countryId.HasValue
                || string.IsNullOrWhiteSpace(name)
                || !countryLookup.TryGetValue(countryId.Value, out var countryName))
            {
                skipped++;
                continue;
            }

            batch.Add(new League(id.Value, countryName, name));
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

    private async Task<(int Imported, IReadOnlyDictionary<int, int> TeamIdMap)> ImportTeamsAsync(
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
        var teamIdMap = new Dictionary<int, int>();

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

            _ = teamFifaApiId;
            teamIdMap[teamApiId.Value] = id.Value;
            batch.Add(new Team(id.Value, longName, shortName));
            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
        addSkipped(skipped);
        logger.LogInformation("Imported teams: {Imported}, skipped: {Skipped}", imported, skipped);
        return (imported, teamIdMap);
    }

    private async Task<(int Imported, IReadOnlyDictionary<int, int> PlayerIdMap)> ImportPlayersAsync(
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
        var playerIdMap = new Dictionary<int, int>();

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

            _ = playerFifaApiId;
            playerIdMap[playerApiId.Value] = id.Value;
            var (firstName, lastName) = SplitPlayerName(name);
            batch.Add(new Player(id.Value, firstName, lastName, birthday, height, weight));
            if (batch.Count >= batchSize)
            {
                imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
            }
        }

        imported += await PersistBatchAsync(dbContext, batch, cancellationToken);
        addSkipped(skipped);
        logger.LogInformation("Imported players: {Imported}, skipped: {Skipped}", imported, skipped);
        return (imported, playerIdMap);
    }

    private async Task<int> ImportMatchesAsync(
        ProFootballDbContext dbContext,
        SqliteConnection sqliteConnection,
        IReadOnlyDictionary<int, string> countryLookup,
        IReadOnlyDictionary<int, int> teamIdMap,
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
                || !homeTeamApiId.HasValue
                || !awayTeamApiId.HasValue
                || string.IsNullOrWhiteSpace(season)
                || !countryLookup.TryGetValue(countryId.Value, out var countryName)
                || !teamIdMap.TryGetValue(homeTeamApiId.Value, out var homeTeamId)
                || !teamIdMap.TryGetValue(awayTeamApiId.Value, out var awayTeamId))
            {
                skipped++;
                continue;
            }

            _ = matchApiId;
            batch.Add(new FootballMatch(
                id.Value,
                countryName,
                leagueId.Value,
                season,
                date.Value,
                homeTeamId,
                awayTeamId,
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
        IReadOnlyDictionary<int, int> teamIdMap,
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

            if (!id.HasValue || !teamApiId.HasValue || !date.HasValue || !teamIdMap.TryGetValue(teamApiId.Value, out var teamId))
            {
                skipped++;
                continue;
            }

            _ = teamFifaApiId;
            batch.Add(new TeamAttribute(
                id.Value,
                teamId,
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
        IReadOnlyDictionary<int, int> playerIdMap,
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

            if (!id.HasValue || !playerApiId.HasValue || !date.HasValue || !playerIdMap.TryGetValue(playerApiId.Value, out var playerId))
            {
                skipped++;
                continue;
            }

            _ = playerFifaApiId;
            batch.Add(new PlayerAttribute(
                id.Value,
                playerId,
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

    private async Task BackfillTeamLeagueLinksAsync(ProFootballDbContext dbContext, CancellationToken cancellationToken)
    {
        if (IsNpgsql(dbContext))
        {
            await dbContext.Database.ExecuteSqlRawAsync("call public.sp_backfill_team_league_links();", cancellationToken);
            return;
        }

        var matches = await dbContext.Matches
            .AsNoTracking()
            .Select(x => new { x.HomeTeamId, x.AwayTeamId, x.LeagueId })
            .ToListAsync(cancellationToken);

        var mapping = matches
            .SelectMany(match => new[]
            {
                new { TeamId = match.HomeTeamId, match.LeagueId },
                new { TeamId = match.AwayTeamId, match.LeagueId },
            })
            .GroupBy(x => new { x.TeamId, x.LeagueId })
            .Select(group => new { group.Key.TeamId, group.Key.LeagueId, Count = group.Count() })
            .ToList();

        var bestLeagueByTeam = mapping
            .GroupBy(x => x.TeamId)
            .Select(group => group.OrderByDescending(x => x.Count).ThenBy(x => x.LeagueId).First())
            .ToDictionary(x => x.TeamId, x => x.LeagueId);

        var teams = await dbContext.Teams.ToListAsync(cancellationToken);
        var teamType = typeof(Team);
        var leagueIdProperty = teamType.GetProperty("LeagueId");

        foreach (var team in teams)
        {
            if (!bestLeagueByTeam.TryGetValue(team.Id, out var leagueId))
            {
                continue;
            }

            leagueIdProperty?.SetValue(team, leagueId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> GenerateMatchEventsAsync(
        ProFootballDbContext dbContext,
        string profile,
        string mode,
        Random random,
        CancellationToken cancellationToken)
    {
        if (mode == "regenerate")
        {
            await dbContext.MatchEvents.ExecuteDeleteAsync(cancellationToken);
        }

        var alreadyGeneratedMatchIds = mode == "append"
            ? await dbContext.MatchEvents.Select(x => x.MatchId).Distinct().ToHashSetAsync(cancellationToken)
            : [];

        var allPlayers = await dbContext.Players.Select(p => p.Id).ToListAsync(cancellationToken);
        if (allPlayers.Count == 0)
        {
            return 0;
        }

        var teamIds = await dbContext.Teams.Select(x => x.Id).ToListAsync(cancellationToken);
        var playersByTeam = teamIds.ToDictionary(x => x, _ => new List<int>());

        var matches = await dbContext.Matches
            .AsNoTracking()
            .OrderBy(m => m.Date)
            .ToListAsync(cancellationToken);

        var events = new List<MatchEvent>(matches.Count * 8);
        var now = DateTime.UtcNow;
        var includeShotsMultiplier = profile switch
        {
            "lite" => 1,
            "realistic" => 2,
            _ => 4,
        };

        foreach (var match in matches)
        {
            if (mode == "append" && alreadyGeneratedMatchIds.Contains(match.Id))
            {
                continue;
            }

            var homePlayers = ResolveTeamPlayers(playersByTeam, allPlayers, match.HomeTeamId);
            var awayPlayers = ResolveTeamPlayers(playersByTeam, allPlayers, match.AwayTeamId);

            var homeGoals = Math.Max(0, match.HomeTeamGoal ?? 0);
            var awayGoals = Math.Max(0, match.AwayTeamGoal ?? 0);

            for (var i = 0; i < homeGoals; i++)
            {
                events.Add(CreateGoalEvent(match.Id, match.HomeTeamId, homePlayers, random, now));
            }

            for (var i = 0; i < awayGoals; i++)
            {
                events.Add(CreateGoalEvent(match.Id, match.AwayTeamId, awayPlayers, random, now));
            }

            var cardEvents = profile switch
            {
                "lite" => random.Next(0, 3),
                "realistic" => random.Next(1, 6),
                _ => random.Next(3, 10),
            };

            for (var i = 0; i < cardEvents; i++)
            {
                var teamId = random.Next(0, 2) == 0 ? match.HomeTeamId : match.AwayTeamId;
                var pool = teamId == match.HomeTeamId ? homePlayers : awayPlayers;
                var eventType = random.NextDouble() < 0.9 ? "yellow_card" : "red_card";
                events.Add(new MatchEvent(
                    id: 0,
                    matchId: match.Id,
                    minute: (short)random.Next(1, 131),
                    eventType: eventType,
                    teamId: teamId,
                    playerId: pool[random.Next(pool.Count)],
                    assistPlayerId: null,
                    payloadJson: """{"source":"synthetic"}""",
                    createdAtUtc: now));
            }

            var substitutionEvents = profile switch
            {
                "lite" => random.Next(2, 5),
                "realistic" => random.Next(4, 9),
                _ => random.Next(8, 15),
            };

            for (var i = 0; i < substitutionEvents; i++)
            {
                var teamId = random.Next(0, 2) == 0 ? match.HomeTeamId : match.AwayTeamId;
                var pool = teamId == match.HomeTeamId ? homePlayers : awayPlayers;
                events.Add(new MatchEvent(
                    id: 0,
                    matchId: match.Id,
                    minute: (short)random.Next(30, 96),
                    eventType: "substitution",
                    teamId: teamId,
                    playerId: pool[random.Next(pool.Count)],
                    assistPlayerId: null,
                    payloadJson: """{"source":"synthetic"}""",
                    createdAtUtc: now));
            }

            var shotEvents = Math.Max(0, (homeGoals + awayGoals + 4) * includeShotsMultiplier);
            for (var i = 0; i < shotEvents; i++)
            {
                var teamId = random.Next(0, 2) == 0 ? match.HomeTeamId : match.AwayTeamId;
                var pool = teamId == match.HomeTeamId ? homePlayers : awayPlayers;
                events.Add(new MatchEvent(
                    id: 0,
                    matchId: match.Id,
                    minute: (short)random.Next(1, 131),
                    eventType: "shot",
                    teamId: teamId,
                    playerId: pool[random.Next(pool.Count)],
                    assistPlayerId: null,
                    payloadJson: """{"source":"synthetic","xg_weight":0.1}""",
                    createdAtUtc: now));
            }
        }

        if (events.Count == 0)
        {
            return 0;
        }

        await dbContext.MatchEvents.AddRangeAsync(events, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return events.Count;
    }

    private async Task<int> RebuildTeamSeasonStatsAsync(ProFootballDbContext dbContext, CancellationToken cancellationToken)
    {
        if (IsNpgsql(dbContext))
        {
            await dbContext.Database.ExecuteSqlRawAsync("call public.sp_rebuild_team_season_stats(null, null);", cancellationToken);
            return await dbContext.TeamSeasonStats.CountAsync(cancellationToken);
        }

        await dbContext.TeamSeasonStats.ExecuteDeleteAsync(cancellationToken);

        var matches = await dbContext.Matches
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var stats = matches
            .SelectMany(match => new[]
            {
                new
                {
                    match.Season,
                    match.LeagueId,
                    TeamId = match.HomeTeamId,
                    GoalsFor = match.HomeTeamGoal ?? 0,
                    GoalsAgainst = match.AwayTeamGoal ?? 0,
                    IsWin = (match.HomeTeamGoal ?? 0) > (match.AwayTeamGoal ?? 0),
                    IsDraw = (match.HomeTeamGoal ?? 0) == (match.AwayTeamGoal ?? 0),
                },
                new
                {
                    match.Season,
                    match.LeagueId,
                    TeamId = match.AwayTeamId,
                    GoalsFor = match.AwayTeamGoal ?? 0,
                    GoalsAgainst = match.HomeTeamGoal ?? 0,
                    IsWin = (match.AwayTeamGoal ?? 0) > (match.HomeTeamGoal ?? 0),
                    IsDraw = (match.AwayTeamGoal ?? 0) == (match.HomeTeamGoal ?? 0),
                },
            })
            .GroupBy(x => new { x.Season, x.LeagueId, x.TeamId })
            .Select(group => new TeamSeasonStat(
                id: 0,
                season: group.Key.Season,
                leagueId: group.Key.LeagueId,
                teamId: group.Key.TeamId,
                matches: group.Count(),
                wins: group.Count(x => x.IsWin),
                draws: group.Count(x => x.IsDraw),
                losses: group.Count(x => !x.IsWin && !x.IsDraw),
                goalsFor: group.Sum(x => x.GoalsFor),
                goalsAgainst: group.Sum(x => x.GoalsAgainst),
                points: group.Count(x => x.IsWin) * 3 + group.Count(x => x.IsDraw)))
            .ToList();

        if (stats.Count == 0)
        {
            return 0;
        }

        await dbContext.TeamSeasonStats.AddRangeAsync(stats, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return stats.Count;
    }


    private async Task<int> ValidateGeneratedDataAsync(ProFootballDbContext dbContext, CancellationToken cancellationToken)
    {
        var errors = 0;

        var goalsByMatch = await dbContext.MatchEvents
            .Where(x => x.EventType == "goal")
            .GroupBy(x => new { x.MatchId, x.TeamId })
            .Select(group => new { group.Key.MatchId, group.Key.TeamId, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var goalLookup = goalsByMatch.ToDictionary(x => (x.MatchId, x.TeamId), x => x.Count);
        var matches = await dbContext.Matches.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var match in matches)
        {
            if (match.HomeTeamId == match.AwayTeamId)
            {
                continue;
            }

            var expectedHome = Math.Max(0, match.HomeTeamGoal ?? 0);
            var expectedAway = Math.Max(0, match.AwayTeamGoal ?? 0);
            var actualHome = goalLookup.GetValueOrDefault((match.Id, match.HomeTeamId), 0);
            var actualAway = goalLookup.GetValueOrDefault((match.Id, match.AwayTeamId), 0);

            if (expectedHome != actualHome || expectedAway != actualAway)
            {
                errors++;
            }
        }

        var invalidEvents = await dbContext.MatchEvents
            .CountAsync(x => x.Minute < 1 || x.Minute > 130 || !EventTypes.Contains(x.EventType), cancellationToken);
        errors += invalidEvents;

        var badTeamSeasonStats = await dbContext.TeamSeasonStats
            .CountAsync(x => (x.Wins + x.Draws + x.Losses) != x.Matches || x.Points != (x.Wins * 3 + x.Draws), cancellationToken);
        errors += badTeamSeasonStats;

        return errors;
    }

    private static MatchEvent CreateGoalEvent(int matchId, int teamId, IReadOnlyList<int> playerPool, Random random, DateTime now)
    {
        short minute = random.NextDouble() switch
        {
            < 0.1 => (short)random.Next(1, 16),
            < 0.4 => (short)random.Next(16, 46),
            < 0.75 => (short)random.Next(46, 76),
            _ => (short)random.Next(76, 131),
        };

        var scorer = playerPool[random.Next(playerPool.Count)];
        int? assist = null;
        if (random.NextDouble() < 0.65 && playerPool.Count > 1)
        {
            assist = scorer;
            while (assist == scorer)
            {
                assist = playerPool[random.Next(playerPool.Count)];
            }
        }

        var payload = $$"""{"source":"synthetic","zone":"central","body_part":"foot","set_piece":false}""";
        return new MatchEvent(0, matchId, minute, "goal", teamId, scorer, assist, payload, now);
    }

    private static List<int> ResolveTeamPlayers(
        IReadOnlyDictionary<int, List<int>> playersByTeam,
        IReadOnlyList<int> allPlayers,
        int teamId)
    {
        if (playersByTeam.TryGetValue(teamId, out var players) && players.Count > 0)
        {
            return players;
        }

        return allPlayers.Take(Math.Min(30, allPlayers.Count)).ToList();
    }

    private static bool IsNpgsql(ProFootballDbContext dbContext)
        => (dbContext.Database.ProviderName ?? string.Empty).Contains("Npgsql", StringComparison.OrdinalIgnoreCase);

    private static (string FirstName, string LastName) SplitPlayerName(string name)
    {
        var normalized = name.Trim();
        var separatorIndex = normalized.LastIndexOf(' ');
        if (separatorIndex <= 0 || separatorIndex >= normalized.Length - 1)
        {
            return (normalized, normalized);
        }

        var firstName = normalized[..separatorIndex].Trim();
        var lastName = normalized[(separatorIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return (normalized, normalized);
        }

        return (firstName, lastName);
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
