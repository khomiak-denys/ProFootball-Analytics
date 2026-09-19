using System.Globalization;
using System.Windows.Data;

namespace ProFootball.Presentation.Converters;

public sealed class DashboardValueToHeightConverter : IValueConverter
{
    private const double MaxHeight = 180d;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double raw)
        {
            return 0d;
        }

        var clamped = Math.Clamp(raw, 0d, 100d);
        return clamped / 100d * MaxHeight;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
