using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace ProFootball.Presentation.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly CultureInfo DefaultCulture = new("en-US");

    private readonly ResourceManager _resourceManager = new(
        "ProFootball.Presentation.Resources.Strings",
        typeof(LocalizationService).Assembly);

    private CultureInfo _currentCulture = DefaultCulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo CurrentCulture => _currentCulture;

    public string this[string key] => GetString(key);

    public void SetCulture(string cultureCode)
    {
        var targetCulture = TryParseCulture(cultureCode) ?? DefaultCulture;
        if (string.Equals(_currentCulture.Name, targetCulture.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _currentCulture = targetCulture;
        CultureInfo.DefaultThreadCurrentCulture = targetCulture;
        CultureInfo.DefaultThreadCurrentUICulture = targetCulture;
        CultureInfo.CurrentCulture = targetCulture;
        CultureInfo.CurrentUICulture = targetCulture;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    private string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        return _resourceManager.GetString(key, _currentCulture)
               ?? _resourceManager.GetString(key, DefaultCulture)
               ?? key;
    }

    private static CultureInfo? TryParseCulture(string? cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
        {
            return null;
        }

        try
        {
            return CultureInfo.GetCultureInfo(cultureCode.Trim());
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
