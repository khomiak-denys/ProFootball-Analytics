namespace ProFootball.Presentation.ViewModels;

public sealed record AnalyticsTopPerformerBarViewModel(
    string PlayerLabel,
    double Value,
    double BarWidth);
