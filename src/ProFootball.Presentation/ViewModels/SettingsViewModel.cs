using ProFootball.Presentation.Services;

namespace ProFootball.Presentation.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private const string ThemeLight = "Light";
    private const string ThemeDark = "Dark";

    private readonly IThemeService _themeService;
    private readonly IAppSettingsService _appSettingsService;

    private string _selectedTheme = ThemeLight;
    private string _selectedLanguage = "Ukrainian";
    private bool _isApplying;

    public SettingsViewModel(IThemeService themeService, IAppSettingsService appSettingsService)
    {
        _themeService = themeService;
        _appSettingsService = appSettingsService;

        ThemeOptions = [ThemeLight, ThemeDark];
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
            _ => AppThemeMode.Light,
        };

        _themeService.SetTheme(targetTheme);
    }
}
