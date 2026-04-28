namespace ProFootball.Presentation.ViewModels;

public sealed record AnalyticsTeamOptionViewModel(
    int TeamApiId,
    string TeamName)
{
    public string DisplayName => TeamName;

    public override string ToString() => DisplayName;
}
