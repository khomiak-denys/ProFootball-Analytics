using ProFootball.Application.Auth.Dtos;
using ProFootball.Infrastructure.Auth;
using Xunit;

namespace ProFootball.Infrastructure.Tests.Features.Auth;

public sealed class DpapiSessionPersistenceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "profootball-session-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAndLoad_ShouldRoundtripSession()
    {
        var service = new DpapiSessionPersistence(_tempDirectory);
        var now = DateTime.UtcNow;
        var session = new PersistedSessionDto(
            PersistedSessionDto.CurrentVersion,
            "manager",
            "John Manager",
            "Admin",
            now);

        await service.SaveAsync(session);
        var loaded = await service.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal(session.Login, loaded!.Login);
        Assert.Equal(session.DisplayName, loaded.DisplayName);
        Assert.Equal(session.Role, loaded.Role);
        Assert.Equal(PersistedSessionDto.CurrentVersion, loaded.Version);
    }

    [Fact]
    public async Task Load_ShouldReturnNull_WhenSessionFileIsMissing()
    {
        var service = new DpapiSessionPersistence(_tempDirectory);

        var loaded = await service.LoadAsync();

        Assert.Null(loaded);
    }

    [Fact]
    public async Task Load_ShouldReturnNullAndCleanup_WhenPayloadIsCorrupted()
    {
        var service = new DpapiSessionPersistence(_tempDirectory);
        Directory.CreateDirectory(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "session.dat");
        await File.WriteAllBytesAsync(filePath, [1, 2, 3, 4, 5]);

        var loaded = await service.LoadAsync();

        Assert.Null(loaded);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task Load_ShouldReturnNullAndCleanup_WhenSchemaVersionIsUnsupported()
    {
        var service = new DpapiSessionPersistence(_tempDirectory);
        var filePath = Path.Combine(_tempDirectory, "session.dat");

        var wrongVersion = new PersistedSessionDto(
            Version: PersistedSessionDto.CurrentVersion + 1,
            Login: "manager",
            DisplayName: "John Manager",
            Role: "Admin",
            CreatedAtUtc: DateTime.UtcNow);
        await service.SaveAsync(wrongVersion);

        var loaded = await service.LoadAsync();

        Assert.Null(loaded);
        Assert.False(File.Exists(filePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
