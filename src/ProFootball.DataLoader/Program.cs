using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Importing.Commands;
using ProFootball.Application.Importing.Dtos;
using ProFootball.Infrastructure;

var sqlitePath = GetArgument(args, "--sqlite");
if (string.IsNullOrWhiteSpace(sqlitePath))
{
    Console.Error.WriteLine("Missing required argument '--sqlite <path-to-database.sqlite>'.");
    return 1;
}

var batchSizeText = GetArgument(args, "--batch-size");
var batchSize = 1_000;
if (!string.IsNullOrWhiteSpace(batchSizeText))
{
    if (!int.TryParse(batchSizeText, out var parsedBatchSize) || parsedBatchSize <= 0)
    {
        Console.Error.WriteLine("Invalid '--batch-size' value. Expected a positive integer.");
        return 1;
    }

    batchSize = parsedBatchSize;
}

using var host = Host.CreateDefaultBuilder(args)
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
        new ImportDataCommand(sqlitePath, batchSize));

    Console.WriteLine("Import completed.");
    Console.WriteLine($"Countries: {result.CountriesImported}");
    Console.WriteLine($"Leagues: {result.LeaguesImported}");
    Console.WriteLine($"Teams: {result.TeamsImported}");
    Console.WriteLine($"Players: {result.PlayersImported}");
    Console.WriteLine($"Matches: {result.MatchesImported}");
    Console.WriteLine($"TeamAttributes: {result.TeamAttributesImported}");
    Console.WriteLine($"PlayerAttributes: {result.PlayerAttributesImported}");
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
