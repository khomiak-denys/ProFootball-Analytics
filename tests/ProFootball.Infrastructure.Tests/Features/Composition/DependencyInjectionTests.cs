using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Commands;
using ProFootball.Application.Common;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Domain.Entities;
using ProFootball.Infrastructure;
using Xunit;

namespace ProFootball.Infrastructure.Tests.Features.Composition;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldThrow_WhenConnectionStringIsMissing()
    {
        var services = new ServiceCollection();
        var configuration = new TestConfiguration(new Dictionary<string, string?>());

        Exception? exception = null;
        try
        {
            services.AddInfrastructure(configuration);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);

        Assert.Contains(DependencyInjection.ConnectionStringName, invalidOperation.Message);
        Assert.Contains(DependencyInjection.ConnectionStringEnvironmentVariable, invalidOperation.Message);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterInfrastructureServices_WhenConnectionStringIsPresent()
    {
        var services = new ServiceCollection();
        var configuration = new TestConfiguration(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = "Host=localhost;Database=profootball;Username=test;Password=test"
        });

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAppUserAuthRepository>());
        Assert.NotNull(provider.GetService<IPasswordHasher>());
        Assert.NotNull(provider.GetService<IQueryDispatcher>());
        Assert.NotNull(provider.GetService<ICommandHandler<RegisterUserCommand, Result>>());
        Assert.NotNull(provider.GetService<IQueryHandler<GetDashboardKpiQuery, DashboardKpiDto>>());
    }

    private sealed class TestConfiguration(Dictionary<string, string?> values) : IConfiguration
    {
        public string? this[string key]
        {
            get => values.TryGetValue(key, out var value) ? value : null;
            set => values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken() => EmptyChangeToken.Instance;

        public IConfigurationSection GetSection(string key) => new TestConfigurationSection(this, key);
    }

    private sealed class TestConfigurationSection(TestConfiguration root, string path) : IConfigurationSection
    {
        public string? this[string key]
        {
            get => root[$"{path}:{key}"];
            set => root[$"{path}:{key}"] = value;
        }

        public string Key => path.Contains(':', StringComparison.Ordinal)
            ? path[(path.LastIndexOf(':') + 1)..]
            : path;

        public string Path => path;

        public string? Value
        {
            get => root[path];
            set => root[path] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken() => EmptyChangeToken.Instance;

        public IConfigurationSection GetSection(string key) => new TestConfigurationSection(root, $"{path}:{key}");
    }

    private sealed class EmptyChangeToken : IChangeToken
    {
        public static EmptyChangeToken Instance { get; } = new();

        public bool HasChanged => false;

        public bool ActiveChangeCallbacks => false;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => NoopDisposable.Instance;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
