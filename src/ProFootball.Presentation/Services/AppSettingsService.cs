using System.IO;
using System.Text.Json;

namespace ProFootball.Presentation.Services;

public interface IAppSettingsService
{
    string GetLanguagePreference();

    void SetLanguagePreference(string language);
}

public sealed class AppSettingsService : IAppSettingsService
{
    private const string DefaultLanguage = "en-US";

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProFootball",
        "app.settings.json");

    public string GetLanguagePreference()
    {
        if (!File.Exists(_settingsPath))
        {
            return DefaultLanguage;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var dto = JsonSerializer.Deserialize<AppSettingsDto>(json);
            if (string.IsNullOrWhiteSpace(dto?.Language))
            {
                return DefaultLanguage;
            }

            return NormalizeLanguage(dto.Language);
        }
        catch
        {
            return DefaultLanguage;
        }
    }

    public void SetLanguagePreference(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var payload = new AppSettingsDto(NormalizeLanguage(language));
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Best effort settings persistence.
        }
    }

    private static string NormalizeLanguage(string? language)
    {
        var value = language?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultLanguage;
        }

        return value.ToLowerInvariant() switch
        {
            "english" => "en-US",
            "ukrainian" => "uk-UA",
            "en-us" => "en-US",
            "uk-ua" => "uk-UA",
            _ => DefaultLanguage,
        };
    }

    private sealed record AppSettingsDto(string Language);
}
