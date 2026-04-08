using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProFootball.Application.Abstractions.Persistence;
using ProFootball.Infrastructure.Persistence;
using ProFootball.Infrastructure.Persistence.Repositories;

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
        services.AddScoped<IClubRepository, EfClubRepository>();

        return services;
    }
}
