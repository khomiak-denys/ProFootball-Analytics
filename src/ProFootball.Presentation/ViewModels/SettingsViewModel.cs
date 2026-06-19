using ProFootball.Presentation.Localization;
using ProFootball.Presentation.Services;

namespace ProFootball.Presentation.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private const string ThemeLight = "Light";
    private const string ThemeDark = "Dark";

    private readonly IThemeService _themeService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly ILocalizationService _localizationService;

    private string _selectedTheme = ThemeLight;
    private string _selectedLanguage = "en-US";
    private bool _isApplying;

    public SettingsViewModel(
        IThemeService themeService,
        IAppSettingsService appSettingsService,
        ILocalizationService localizationService)
    {
        _themeService = themeService;
        _appSettingsService = appSettingsService;
        _localizationService = localizationService;

        ThemeOptions = [ThemeLight, ThemeDark];
        LanguageOptions =
        [
            new LanguageOption("en-US", "English (en-US)"),
            new LanguageOption("uk-UA", "Українська (uk-UA)")
        ];

        Initialize();
    }

    public IReadOnlyList<string> ThemeOptions { get; }

    public IReadOnlyList<LanguageOption> LanguageOptions { get; }

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
            _localizationService.SetCulture(value);
        }
    }

    private void Initialize()
    {
        _isApplying = true;
        try
        {
            SelectedTheme = _themeService.CurrentTheme == AppThemeMode.Dark ? ThemeDark : ThemeLight;
            var storedLanguage = _appSettingsService.GetLanguagePreference();
            SelectedLanguage = LanguageOptions.Any(item => string.Equals(item.Code, storedLanguage, StringComparison.OrdinalIgnoreCase))
                ? LanguageOptions.First(item => string.Equals(item.Code, storedLanguage, StringComparison.OrdinalIgnoreCase)).Code
                : LanguageOptions[0].Code;
            _localizationService.SetCulture(SelectedLanguage);
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

    public sealed record LanguageOption(string Code, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
