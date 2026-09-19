namespace ProFootball.Presentation.ViewModels;

public sealed record RatingDeltaItemViewModel(
    string PlayerName,
    int Overall,
    int Potential,
    int Delta);
