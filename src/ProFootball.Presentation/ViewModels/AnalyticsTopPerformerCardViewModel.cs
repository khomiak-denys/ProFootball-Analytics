namespace ProFootball.Presentation.ViewModels;

public sealed record AnalyticsTopPerformerCardViewModel(
    int Rank,
    string PlayerName,
    double Rating,
    double Potential,
    int Samples);
