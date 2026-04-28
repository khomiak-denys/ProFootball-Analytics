using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ProFootball.Presentation.Localization;

[MarkupExtensionReturnType(typeof(object))]
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var source = System.Windows.Application.Current?.Resources["LocalizationService"] as ILocalizationService;
        if (source is null)
        {
            return Key;
        }

        var binding = new Binding($"[{Key}]")
        {
            Source = source,
            Mode = BindingMode.OneWay,
            FallbackValue = Key,
            TargetNullValue = Key,
        };

        return binding.ProvideValue(serviceProvider);
    }
}
