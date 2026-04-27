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
    private const string DefaultLanguage = "Ukrainian";

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

            return dto.Language.Trim();
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

            var payload = new AppSettingsDto(language.Trim());
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Best effort settings persistence.
        }
    }

    private sealed record AppSettingsDto(string Language);
}
