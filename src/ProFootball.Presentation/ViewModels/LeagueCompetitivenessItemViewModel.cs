namespace ProFootball.Presentation.ViewModels;

public sealed record LeagueCompetitivenessItemViewModel(
    string LeagueName,
    string CountryName,
    int MatchCount,
    double DrawRate,
    double AverageGoalDifference);
