using System.ComponentModel;
using System.Globalization;

namespace ProFootball.Presentation.Localization;

public interface ILocalizationService : INotifyPropertyChanged
{
    CultureInfo CurrentCulture { get; }

    string this[string key] { get; }

    void SetCulture(string cultureCode);
}
