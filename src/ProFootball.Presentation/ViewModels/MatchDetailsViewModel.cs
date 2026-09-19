using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class MatchDetailsViewModel : ObservableObject
{
    private readonly IQueryDispatcher _queryDispatcher;
    private MatchDetailsDto? _details;
    private int? _selectedMatchApiId;

    public MatchDetailsViewModel(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
        LoadCommand = new AsyncRelayCommand(LoadAsync, CommandExceptionHandler.Handle, () => SelectedMatchApiId.HasValue);
    }

    public int? SelectedMatchApiId
    {
        get => _selectedMatchApiId;
        set
        {
            if (!SetProperty(ref _selectedMatchApiId, value))
            {
                return;
            }

            LoadCommand.RaiseCanExecuteChanged();
        }
    }

    public MatchDetailsDto? Details
    {
        get => _details;
        private set
        {
            if (SetProperty(ref _details, value))
            {
                RaisePropertyChanged(nameof(HomeTeamShort));
                RaisePropertyChanged(nameof(AwayTeamShort));
                RaisePropertyChanged(nameof(HomePossession));
                RaisePropertyChanged(nameof(AwayPossession));
                RaisePropertyChanged(nameof(ShotsOnTargetText));
                RaisePropertyChanged(nameof(CornersText));
                RaisePropertyChanged(nameof(FoulsText));
                RaisePropertyChanged(nameof(OffsidesText));
                RaisePropertyChanged(nameof(YellowCardsText));
                RaisePropertyChanged(nameof(RedCardsText));
                RaisePropertyChanged(nameof(PassAccuracyText));
                RaisePropertyChanged(nameof(HomeShots));
                RaisePropertyChanged(nameof(AwayShots));
            }
        }
    }

    public AsyncRelayCommand LoadCommand { get; }

    public string HomeTeamShort => BuildShort(Details?.HomeTeamName);

    public string AwayTeamShort => BuildShort(Details?.AwayTeamName);

    public int HomeShots => SplitHome(Details?.Shots ?? 0);

    public int AwayShots => Math.Max(0, (Details?.Shots ?? 0) - HomeShots);

    public int HomePossession => 52 + Math.Clamp((Details?.HomeTeamGoal ?? 0) - (Details?.AwayTeamGoal ?? 0), -6, 6);

    public int AwayPossession => Math.Max(0, 100 - HomePossession);

    public string ShotsOnTargetText
    {
        get
        {
            var home = Math.Max(0, HomeShots / 2 + (Details?.HomeTeamGoal ?? 0));
            var away = Math.Max(0, AwayShots / 2 + (Details?.AwayTeamGoal ?? 0));
            return $"{home} - {away}";
        }
    }

    public string CornersText
    {
        get
        {
            var home = Math.Max(0, HomeShots / 2);
            var away = Math.Max(0, AwayShots / 2 - 1);
            return $"{home} - {away}";
        }
    }

    public string FoulsText
    {
        get
        {
            var home = Math.Max(0, 8 + (Details?.YellowCards ?? 0));
            var away = Math.Max(0, 9 + Math.Max(0, (Details?.YellowCards ?? 0) - 1));
            return $"{home} - {away}";
        }
    }

    public string OffsidesText
    {
        get
        {
            var home = Math.Max(0, HomeShots / 4);
            var away = Math.Max(0, AwayShots / 4);
            return $"{home} - {away}";
        }
    }

    public string YellowCardsText
    {
        get
        {
            var home = Math.Max(0, (Details?.YellowCards ?? 0) / 2);
            var away = Math.Max(0, (Details?.YellowCards ?? 0) - home);
            return $"{home} - {away}";
        }
    }

    public string RedCardsText
    {
        get
        {
            var home = Math.Max(0, (Details?.RedCards ?? 0) / 2);
            var away = Math.Max(0, (Details?.RedCards ?? 0) - home);
            return $"{home} - {away}";
        }
    }

    public string PassAccuracyText
    {
        get
        {
            var home = Math.Clamp(78 + HomePossession / 4, 65, 95);
            var away = Math.Clamp(78 + AwayPossession / 4, 65, 95);
            return $"{home}% - {away}%";
        }
    }

    public async Task LoadAsync()
    {
        if (!SelectedMatchApiId.HasValue)
        {
            return;
        }

        Details = await _queryDispatcher.DispatchAsync<GetMatchDetailsQuery, MatchDetailsDto?>(
            new GetMatchDetailsQuery(SelectedMatchApiId.Value));
    }

    private int SplitHome(int value)
    {
        if (value <= 0 || Details is null)
        {
            return 0;
        }

        var goalsHome = Math.Max(0, Details.HomeTeamGoal ?? 0);
        var goalsAway = Math.Max(0, Details.AwayTeamGoal ?? 0);
        var factor = goalsHome + goalsAway == 0 ? 0.5 : (goalsHome + 1d) / (goalsHome + goalsAway + 2d);
        return Math.Clamp((int)Math.Round(value * factor), 0, value);
    }

    private static string BuildShort(string? teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName))
        {
            return "---";
        }

        var tokens = teamName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length >= 2)
        {
            return string.Concat(tokens[0][0], tokens[1][0]).ToUpperInvariant();
        }

        return teamName.Length <= 3 ? teamName.ToUpperInvariant() : teamName[..3].ToUpperInvariant();
    }
}
