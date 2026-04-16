using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProFootball.Infrastructure.Persistence;

public sealed class ProFootballDbContextFactory : IDesignTimeDbContextFactory<ProFootballDbContext>
{
    public ProFootballDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(DependencyInjection.ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Design-time DbContext requires an explicit connection string. " +
                $"Set environment variable '{DependencyInjection.ConnectionStringEnvironmentVariable}'.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ProFootballDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ProFootballDbContext(optionsBuilder.Options);
    }
}
