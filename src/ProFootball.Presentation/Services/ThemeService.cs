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

        if (!app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(() => ApplyThemeDictionary(theme));
            return;
        }

        var themeSource = new Uri(theme == AppThemeMode.Light ? LightDictionary : DarkDictionary, UriKind.Relative);
        var materialThemeSource = new Uri(
            theme == AppThemeMode.Light ? MaterialDesignLightDictionary : MaterialDesignDarkDictionary,
            UriKind.Absolute);
        var resources = app.Resources.MergedDictionaries;

        UpdateOrInsertDictionary(resources, IsMaterialThemeDictionary, materialThemeSource, 0);
        UpdateOrInsertDictionary(resources, IsAppThemeDictionary, themeSource, resources.Count);
    }

    private static void UpdateOrInsertDictionary(
        ICollection<ResourceDictionary> dictionaries,
        Func<ResourceDictionary, bool> selector,
        Uri source,
        int insertIndex)
    {
        if (dictionaries is not IList<ResourceDictionary> list)
        {
            return;
        }

        var existing = list.FirstOrDefault(selector);
        if (existing is null)
        {
            list.Insert(Math.Clamp(insertIndex, 0, list.Count), new ResourceDictionary { Source = source });
            return;
        }

        if (existing.Source != source)
        {
            existing.Source = source;
        }
    }

    private static bool IsMaterialThemeDictionary(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString;
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        return source.EndsWith("MaterialDesignTheme.Light.xaml", StringComparison.OrdinalIgnoreCase) ||
               source.EndsWith("MaterialDesignTheme.Dark.xaml", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAppThemeDictionary(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString;
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        // Match only project theme dictionaries and exclude MaterialDesign theme dictionaries.
        if (source.Contains("MaterialDesignTheme.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return source.EndsWith("/Themes/Theme.Light.xaml", StringComparison.OrdinalIgnoreCase) ||
               source.EndsWith("/Themes/Theme.Dark.xaml", StringComparison.OrdinalIgnoreCase) ||
               source.EndsWith("Themes/Theme.Light.xaml", StringComparison.OrdinalIgnoreCase) ||
               source.EndsWith("Themes/Theme.Dark.xaml", StringComparison.OrdinalIgnoreCase);
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
