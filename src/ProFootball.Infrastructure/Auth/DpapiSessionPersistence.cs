using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ProFootball.Application.Auth.Abstractions;
using ProFootball.Application.Auth.Dtos;

namespace ProFootball.Infrastructure.Auth;

public sealed class DpapiSessionPersistence : ISessionPersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _sessionFilePath;

    public DpapiSessionPersistence(string? rootDirectory = null)
    {
        var baseDirectory = string.IsNullOrWhiteSpace(rootDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProFootball")
            : rootDirectory;
        _sessionFilePath = Path.Combine(baseDirectory, "session.dat");
    }

    public async Task SaveAsync(PersistedSessionDto session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(_sessionFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(session, JsonOptions));
        var protectedBytes = ProtectedData.Protect(payloadBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(_sessionFilePath, protectedBytes, cancellationToken);
    }

    public async Task<PersistedSessionDto?> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        if (!File.Exists(_sessionFilePath))
        {
            return null;
        }

        try
        {
            var protectedBytes = await File.ReadAllBytesAsync(_sessionFilePath, cancellationToken);
            if (protectedBytes.Length == 0)
            {
                await ClearAsync(cancellationToken);
                return null;
            }

            var payloadBytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            var payload = JsonSerializer.Deserialize<PersistedSessionDto>(payloadBytes, JsonOptions);
            if (payload is null || payload.Version != PersistedSessionDto.CurrentVersion || string.IsNullOrWhiteSpace(payload.Login))
            {
                await ClearAsync(cancellationToken);
                return null;
            }

            return payload;
        }
        catch (CryptographicException)
        {
            await ClearAsync(cancellationToken);
            return null;
        }
        catch (JsonException)
        {
            await ClearAsync(cancellationToken);
            return null;
        }
        catch (IOException)
        {
            await ClearAsync(cancellationToken);
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            await ClearAsync(cancellationToken);
            return null;
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (File.Exists(_sessionFilePath))
            {
                File.Delete(_sessionFilePath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return Task.CompletedTask;
    }
}
