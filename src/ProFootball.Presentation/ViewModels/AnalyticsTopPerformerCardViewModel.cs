namespace ProFootball.Presentation.ViewModels;

public sealed record AnalyticsTopPerformerCardViewModel(
    int Rank,
    string PlayerName,
    double Rating,
    int Goals,
    int Assists);
