using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProFootball.Infrastructure.Persistence;

public sealed class ProFootballDbContextFactory : IDesignTimeDbContextFactory<ProFootballDbContext>
{
    public ProFootballDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("PROFOOTBALL_DB_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=profootball;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ProFootballDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ProFootballDbContext(optionsBuilder.Options);
    }
}
