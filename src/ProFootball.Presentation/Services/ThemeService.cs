using System.IO;
using System.Text.Json;
using System.Windows;

namespace ProFootball.Presentation.Services;

public interface IThemeService
{
    AppThemeMode CurrentTheme { get; }
    void Initialize();
    void SetTheme(AppThemeMode theme);
    void ToggleTheme();
}

public sealed class ThemeService : IThemeService
{
    private const string LightDictionary = "Themes/Theme.Light.xaml";
    private const string DarkDictionary = "Themes/Theme.Dark.xaml";
    private const string MaterialDesignLightDictionary = "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml";
    private const string MaterialDesignDarkDictionary = "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml";

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProFootball",
        "theme.settings.json");

    public AppThemeMode CurrentTheme { get; private set; } = AppThemeMode.Light;

    public void Initialize()
    {
        var theme = ReadThemeFromStorage() ?? AppThemeMode.Light;
        SetTheme(theme);
    }

    public void ToggleTheme()
    {
        SetTheme(CurrentTheme == AppThemeMode.Light ? AppThemeMode.Dark : AppThemeMode.Light);
    }

    public void SetTheme(AppThemeMode theme)
    {
        CurrentTheme = theme;
        ApplyThemeDictionary(theme);
        PersistTheme(theme);
    }

    private static void ApplyThemeDictionary(AppThemeMode theme)
    {
        var app = System.Windows.Application.Current;
        if (app is null)
        {
            return;
        }

        var themeSource = new Uri(theme == AppThemeMode.Light ? LightDictionary : DarkDictionary, UriKind.Relative);
        var materialThemeSource = new Uri(
            theme == AppThemeMode.Light ? MaterialDesignLightDictionary : MaterialDesignDarkDictionary,
            UriKind.Absolute);
        var resources = app.Resources.MergedDictionaries;

        var existingTheme = resources.FirstOrDefault(dict =>
            dict.Source is not null &&
            (dict.Source.OriginalString.EndsWith("Theme.Light.xaml", StringComparison.OrdinalIgnoreCase)
             || dict.Source.OriginalString.EndsWith("Theme.Dark.xaml", StringComparison.OrdinalIgnoreCase)));

        if (existingTheme is null)
        {
            resources.Insert(0, new ResourceDictionary { Source = themeSource });
            return;
        }

        if (existingTheme.Source == themeSource)
        {
            // Keep going - material dictionary may still need update.
        }
        else
        {
            existingTheme.Source = themeSource;
        }

        var existingMaterialTheme = resources.FirstOrDefault(dict =>
            dict.Source is not null &&
            (dict.Source.OriginalString.EndsWith("MaterialDesignTheme.Light.xaml", StringComparison.OrdinalIgnoreCase)
             || dict.Source.OriginalString.EndsWith("MaterialDesignTheme.Dark.xaml", StringComparison.OrdinalIgnoreCase)));

        if (existingMaterialTheme is null)
        {
            resources.Insert(0, new ResourceDictionary { Source = materialThemeSource });
            return;
        }

        if (existingMaterialTheme.Source == materialThemeSource)
        {
            return;
        }

        existingMaterialTheme.Source = materialThemeSource;
    }

    private AppThemeMode? ReadThemeFromStorage()
    {
        if (!File.Exists(_settingsPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var dto = JsonSerializer.Deserialize<ThemeSettingsDto>(json);
            if (dto?.Theme is null)
            {
                return null;
            }

            return Enum.TryParse<AppThemeMode>(dto.Theme, ignoreCase: true, out var theme)
                ? theme
                : null;
        }
        catch
        {
            return null;
        }
    }

    private void PersistTheme(AppThemeMode theme)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(new ThemeSettingsDto(theme.ToString()), new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Best effort persistence.
        }
    }

    private sealed record ThemeSettingsDto(string Theme);
}
