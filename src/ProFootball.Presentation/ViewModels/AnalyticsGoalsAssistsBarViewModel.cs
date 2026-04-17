namespace ProFootball.Presentation.ViewModels;

public sealed record AnalyticsGoalsAssistsBarViewModel(
    string PlayerLabel,
    int Goals,
    int Assists,
    double GoalsBarHeight,
    double AssistsBarHeight);
