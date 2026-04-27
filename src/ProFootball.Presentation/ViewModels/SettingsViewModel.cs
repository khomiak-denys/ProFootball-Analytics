using ProFootball.Presentation.Services;

namespace ProFootball.Presentation.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private const string ThemeSystem = "System";
    private const string ThemeLight = "Light";
    private const string ThemeDark = "Dark";

    private readonly IThemeService _themeService;
    private readonly IAppSettingsService _appSettingsService;

    private string _selectedTheme = ThemeSystem;
    private string _selectedLanguage = "Ukrainian";
    private bool _isApplying;

    public SettingsViewModel(IThemeService themeService, IAppSettingsService appSettingsService)
    {
        _themeService = themeService;
        _appSettingsService = appSettingsService;

        ThemeOptions = [ThemeSystem, ThemeLight, ThemeDark];
        LanguageOptions = ["Ukrainian", "English"];

        Initialize();
    }

    public IReadOnlyList<string> ThemeOptions { get; }

    public IReadOnlyList<string> LanguageOptions { get; }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (!SetProperty(ref _selectedTheme, value) || _isApplying)
            {
                return;
            }

            ApplyTheme(value);
        }
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!SetProperty(ref _selectedLanguage, value))
            {
                return;
            }

            _appSettingsService.SetLanguagePreference(value);
        }
    }

    private void Initialize()
    {
        _isApplying = true;
        try
        {
            SelectedTheme = _themeService.CurrentTheme == AppThemeMode.Dark ? ThemeDark : ThemeLight;
            var storedLanguage = _appSettingsService.GetLanguagePreference();
            SelectedLanguage = LanguageOptions.Contains(storedLanguage, StringComparer.OrdinalIgnoreCase)
                ? LanguageOptions.First(item => string.Equals(item, storedLanguage, StringComparison.OrdinalIgnoreCase))
                : LanguageOptions[0];
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void ApplyTheme(string value)
    {
        var targetTheme = value switch
        {
            ThemeDark => AppThemeMode.Dark,
            ThemeLight => AppThemeMode.Light,
            _ => GetSystemThemePreference(),
        };

        _themeService.SetTheme(targetTheme);
    }

    private static AppThemeMode GetSystemThemePreference()
    {
        const string personalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string lightThemeValueName = "AppsUseLightTheme";

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(personalizePath, writable: false);
            if (key?.GetValue(lightThemeValueName) is int rawValue)
            {
                return rawValue == 0 ? AppThemeMode.Dark : AppThemeMode.Light;
            }
        }
        catch
        {
            // Keep fallback default.
        }

        return AppThemeMode.Light;
    }
}
