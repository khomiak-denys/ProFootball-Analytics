using System.Globalization;

namespace ProFootball.Presentation.ViewModels;

public sealed record AnalyticsPlayerOptionViewModel(
    int PlayerApiId,
    string PlayerName,
    double AverageOverallRating,
    double AveragePotential,
    int Samples)
{
    public string DisplayName => string.Create(
        CultureInfo.InvariantCulture,
        $"{PlayerName} ({Math.Round(AverageOverallRating, MidpointRounding.AwayFromZero):0})");
}
