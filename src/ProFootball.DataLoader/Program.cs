using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Importing.Commands;
using ProFootball.Application.Importing.Dtos;
using ProFootball.Infrastructure;
using Serilog;
using Serilog.Events;

var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsDirectory);
var logFilePath = Path.Combine(logsDirectory, "profootball-.log");
#if DEBUG
const LogEventLevel minimumLogLevel = LogEventLevel.Debug;
#else
const LogEventLevel minimumLogLevel = LogEventLevel.Warning;
#endif
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(minimumLogLevel)
    .Enrich.FromLogContext()
    .WriteTo.File(
        logFilePath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var sqlitePath = GetArgument(args, "--sqlite");
    if (string.IsNullOrWhiteSpace(sqlitePath))
    {
        Console.Error.WriteLine("Missing required argument '--sqlite <path-to-database.sqlite>'.");
        Log.Error("Missing required argument '--sqlite <path-to-database.sqlite>'.");
        return 1;
    }

    var batchSizeText = GetArgument(args, "--batch-size");
    var batchSize = 1_000;
    if (!string.IsNullOrWhiteSpace(batchSizeText))
    {
        if (!int.TryParse(batchSizeText, out var parsedBatchSize) || parsedBatchSize <= 0)
        {
            Console.Error.WriteLine("Invalid '--batch-size' value. Expected a positive integer.");
            Log.Error("Invalid '--batch-size' value: {BatchSizeText}", batchSizeText);
            return 1;
        }

        batchSize = parsedBatchSize;
    }

    var profile = GetArgument(args, "--profile") ?? "realistic";
    var mode = GetArgument(args, "--mode") ?? "regenerate";

    using var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.SetBasePath(AppContext.BaseDirectory);
            configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            configurationBuilder.AddEnvironmentVariables();
        })
        .ConfigureServices((context, services) =>
        {
            services.AddInfrastructure(context.Configuration);
        })
        .Build();

    var started = false;
    try
    {
        await host.StartAsync();
        started = true;

        using var scope = host.Services.CreateScope();
        var commandDispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var result = await commandDispatcher.DispatchAsync<ImportDataCommand, DataImportResult>(
            new ImportDataCommand(sqlitePath, batchSize, profile, mode));

        Console.WriteLine("Import completed.");
        Console.WriteLine($"CountriesResolved: {result.CountriesResolved}");
        Console.WriteLine($"Leagues: {result.LeaguesImported}");
        Console.WriteLine($"Teams: {result.TeamsImported}");
        Console.WriteLine($"Players: {result.PlayersImported}");
        Console.WriteLine($"Matches: {result.MatchesImported}");
        Console.WriteLine($"TeamAttributes: {result.TeamAttributesImported}");
        Console.WriteLine($"PlayerAttributes: {result.PlayerAttributesImported}");
        Console.WriteLine($"MatchEvents: {result.MatchEventsGenerated}");
        Console.WriteLine($"PlayerMatchStats: {result.PlayerMatchStatsGenerated}");
        Console.WriteLine($"TeamSeasonStats: {result.TeamSeasonStatsRebuilt}");
        Console.WriteLine($"AnalyticsFactDaily: {result.AnalyticsFactsRebuilt}");
        Console.WriteLine($"ValidationErrors: {result.ValidationErrors}");
        Console.WriteLine($"SkippedRows: {result.SkippedRows}");
        Console.WriteLine($"Duration: {result.Duration}");

        return 0;
    }
    finally
    {
        if (started)
        {
            await host.StopAsync(TimeSpan.FromSeconds(5));
        }
    }
}
catch (Exception exception)
{
    Log.Fatal(exception, "Data import failed.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

static string? GetArgument(IReadOnlyList<string> args, string key)
{
    for (var i = 0; i < args.Count; i++)
    {
        if (!string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (i + 1 < args.Count)
        {
            return args[i + 1];
        }

        return null;
    }

    return null;
}
