using ProFootball.Infrastructure.Persistence;
using Xunit;

namespace ProFootball.Infrastructure.Tests.Features.Persistence;

public class ProFootballDbContextFactoryTests
{
    private static readonly object EnvironmentLock = new();

    [Fact]
    public void CreateDbContext_ShouldThrow_WhenEnvironmentVariableIsMissing()
    {
        lock (EnvironmentLock)
        {
            var previous = Environment.GetEnvironmentVariable(DependencyInjection.ConnectionStringEnvironmentVariable);
            try
            {
                Environment.SetEnvironmentVariable(DependencyInjection.ConnectionStringEnvironmentVariable, null);
                var factory = new ProFootballDbContextFactory();

                var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext([]));

                Assert.Contains(DependencyInjection.ConnectionStringEnvironmentVariable, exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable(DependencyInjection.ConnectionStringEnvironmentVariable, previous);
            }
        }
    }

    [Fact]
    public void CreateDbContext_ShouldCreateNpgsqlContext_WhenEnvironmentVariableIsSet()
    {
        lock (EnvironmentLock)
        {
            var previous = Environment.GetEnvironmentVariable(DependencyInjection.ConnectionStringEnvironmentVariable);
            try
            {
                Environment.SetEnvironmentVariable(
                    DependencyInjection.ConnectionStringEnvironmentVariable,
                    "Host=localhost;Database=profootball;Username=test;Password=test");

                var factory = new ProFootballDbContextFactory();
                using var dbContext = factory.CreateDbContext([]);

                Assert.Contains("Npgsql", dbContext.Database.ProviderName, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Environment.SetEnvironmentVariable(DependencyInjection.ConnectionStringEnvironmentVariable, previous);
            }
        }
    }
}
