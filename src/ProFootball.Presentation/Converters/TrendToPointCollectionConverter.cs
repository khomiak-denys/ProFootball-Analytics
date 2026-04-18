using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ProFootball.Application.Player.Dtos;

namespace ProFootball.Presentation.Converters;

public sealed class TrendToPointCollectionConverter : IValueConverter
{
    private const double ChartWidth = 380;
    private const double ChartHeight = 160;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<PlayerTrendPointDto> points)
        {
            return new PointCollection();
        }

        var mode = parameter?.ToString()?.Trim().ToLowerInvariant() ?? "overall";
        var list = points.ToList();
        if (list.Count == 0)
        {
            return new PointCollection();
        }

        var xStep = list.Count == 1 ? 0 : ChartWidth / (list.Count - 1d);
        var result = new PointCollection();

        for (var i = 0; i < list.Count; i++)
        {
            var metric = mode switch
            {
                "potential" => list[i].Potential,
                _ => list[i].OverallRating,
            };

            if (!metric.HasValue)
            {
                continue;
            }

            var x = i * xStep;
            var clamped = Math.Clamp(metric.Value, 0, 100);
            var y = ChartHeight - (clamped / 100d * ChartHeight);
            result.Add(new System.Windows.Point(x, y));
        }

        return result;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
