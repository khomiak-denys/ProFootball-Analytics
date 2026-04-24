namespace ProFootball.Presentation.ViewModels;

public sealed record AnalyticsGoalsAssistsBarViewModel(
    string PlayerLabel,
    double Overall,
    double Potential,
    double OverallBarHeight,
    double PotentialBarHeight);
