using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProFootball.Application.Abstractions.Persistence;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Infrastructure.Importing;
using ProFootball.Infrastructure.Persistence;
using ProFootball.Infrastructure.Persistence.Repositories;
using ProFootball.Infrastructure.Querying;

namespace ProFootball.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ProFootballDb";
    public const string ConnectionStringEnvironmentVariable = "PROFOOTBALL_DB_CONNECTION_STRING";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
                               ?? configuration[ConnectionStringEnvironmentVariable];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. " +
                $"Set appsettings.json or env var '{ConnectionStringEnvironmentVariable}'.");
        }

        services.AddDbContext<ProFootballDbContext>(options => options.UseNpgsql(connectionString));
        services.AddDbContextFactory<ProFootballDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IClubRepository, EfClubRepository>();
        services.AddScoped<ICountriesLeaguesQueryService, CountriesLeaguesQueryService>();
        services.AddScoped<ITeamsQueryService, TeamsQueryService>();
        services.AddScoped<IPlayersQueryService, PlayersQueryService>();
        services.AddScoped<IMatchesQueryService, MatchesQueryService>();
        services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();
        services.AddScoped<IDataImportService, DataImportService>();

        return services;
    }
}
