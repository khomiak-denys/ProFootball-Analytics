using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Data;

namespace ProFootball.Presentation.Converters;

public sealed class NullableIntTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int number => number.ToString(culture),
            _ => string.Empty,
        };
    }

    [return: MaybeNull]
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value?.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return int.TryParse(text.Trim(), NumberStyles.Integer, culture, out var parsed)
            ? parsed
            : null;
    }
}
