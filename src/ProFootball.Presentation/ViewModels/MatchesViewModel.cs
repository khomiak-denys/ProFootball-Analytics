using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.League.Dtos;
using ProFootball.Application.League.Queries;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class MatchesViewModel : ObservableObject
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly Action<int> _openDetails;
    private MatchListItemDto? _selectedMatch;
    private LeagueDto? _selectedLeague;
    private string? _season;
    private int? _teamApiId;
    private int _page = 1;
    private int _pageSize = 25;
    private int _totalCount;

    public MatchesViewModel(
        IQueryDispatcher queryDispatcher,
        Action<int> openDetails)
    {
        _queryDispatcher = queryDispatcher;
        _openDetails = openDetails;

        Matches = new ObservableCollection<MatchListItemDto>();
        Leagues = new ObservableCollection<LeagueDto>();
        SearchCommand = new AsyncRelayCommand(StartSearchAsync, CommandExceptionHandler.Handle);
        LoadLeaguesCommand = new AsyncRelayCommand(LoadLeaguesAsync, CommandExceptionHandler.Handle);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, CommandExceptionHandler.Handle, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, CommandExceptionHandler.Handle, () => HasPreviousPage);
        OpenDetailsCommand = new RelayCommand(OpenDetails, () => SelectedMatch is not null);
    }

    public ObservableCollection<MatchListItemDto> Matches { get; }

    public ObservableCollection<LeagueDto> Leagues { get; }

    public MatchListItemDto? SelectedMatch
    {
        get => _selectedMatch;
        set
        {
            if (!SetProperty(ref _selectedMatch, value))
            {
                return;
            }

            OpenDetailsCommand.RaiseCanExecuteChanged();
            RaiseSelectedMatchPropertiesChanged();
        }
    }

    public string SelectedMatchCompetition => SelectedMatch?.LeagueName ?? "--";

    public string SelectedMatchCountry => SelectedMatch?.CountryName ?? "--";

    public string SelectedMatchSeason => SelectedMatch?.Season ?? "--";

    public string SelectedMatchDate => SelectedMatch?.Date.ToString("yyyy-MM-dd") ?? "--";

    public string SelectedMatchStage => SelectedMatch is null
        ? "--"
        : $"Matchday {Math.Abs(SelectedMatch.MatchApiId % 38) + 1}";

    public string SelectedMatchHomeTeam => SelectedMatch?.HomeTeamName ?? "Home";

    public string SelectedMatchAwayTeam => SelectedMatch?.AwayTeamName ?? "Away";

    public string SelectedMatchScore => SelectedMatch is null
        ? "--"
        : $"{FormatScore(SelectedMatch.HomeTeamGoal)} - {FormatScore(SelectedMatch.AwayTeamGoal)}";

    public int SelectedMatchGoals => GetGoalTotal();

    public int SelectedMatchAssists => Math.Max(0, GetGoalTotal() - 1) + 1;

    public int SelectedMatchShots => SelectedMatch is null ? 0 : Math.Max(8, GetGoalTotal() * 8 + 10);

    public int SelectedMatchPasses => SelectedMatch is null ? 0 : Math.Max(120, GetGoalTotal() * 85 + 120);

    public IReadOnlyList<MatchStatisticViewModel> MatchStatistics => BuildMatchStatistics();

    public LeagueDto? SelectedLeague
    {
        get => _selectedLeague;
        set => SetProperty(ref _selectedLeague, value);
    }

    public string? Season
    {
        get => _season;
        set => SetProperty(ref _season, value);
    }

    public int? TeamApiId
    {
        get => _teamApiId;
        set => SetProperty(ref _teamApiId, value);
    }

    public int Page
    {
        get => _page;
        private set
        {
            if (SetProperty(ref _page, value))
            {
                RaisePropertyChanged(nameof(HasPreviousPage));
                RaisePropertyChanged(nameof(HasNextPage));
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                RaisePropertyChanged(nameof(HasPreviousPage));
                RaisePropertyChanged(nameof(HasNextPage));
                NextPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set
        {
            if (SetProperty(ref _totalCount, value))
            {
                RaisePropertyChanged(nameof(HasNextPage));
            }
        }
    }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page * PageSize < TotalCount;

    public AsyncRelayCommand SearchCommand { get; }

    public AsyncRelayCommand LoadLeaguesCommand { get; }

    public AsyncRelayCommand NextPageCommand { get; }

    public AsyncRelayCommand PreviousPageCommand { get; }

    public RelayCommand OpenDetailsCommand { get; }

    private async Task StartSearchAsync()
    {
        Page = 1;
        await SearchAsync();
    }

    public async Task LoadLeaguesAsync()
    {
        Leagues.Clear();
        var leagues = await _queryDispatcher.DispatchAsync<GetLeaguesQuery, IReadOnlyList<LeagueDto>>(new GetLeaguesQuery());
        foreach (var league in leagues)
        {
            Leagues.Add(league);
        }
    }

    public async Task SearchAsync()
    {
        var result = await _queryDispatcher.DispatchAsync<MatchSearchQuery, PagedResult<MatchListItemDto>>(new MatchSearchQuery(
            SelectedLeague?.Id,
            string.IsNullOrWhiteSpace(Season) ? null : Season.Trim(),
            TeamApiId,
            null,
            null,
            "date",
            true,
            Page,
            PageSize));

        TotalCount = result.TotalCount;
        Matches.Clear();
        foreach (var item in result.Items)
        {
            Matches.Add(item);
        }

        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
    }

    private async Task NextPageAsync()
    {
        if (!HasNextPage)
        {
            return;
        }

        Page++;
        await SearchAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (!HasPreviousPage)
        {
            return;
        }

        Page--;
        await SearchAsync();
    }

    private void OpenDetails()
    {
        if (SelectedMatch is null)
        {
            return;
        }

        _openDetails(SelectedMatch.MatchApiId);
    }

    private void RaiseSelectedMatchPropertiesChanged()
    {
        RaisePropertyChanged(nameof(SelectedMatchCompetition));
        RaisePropertyChanged(nameof(SelectedMatchCountry));
        RaisePropertyChanged(nameof(SelectedMatchSeason));
        RaisePropertyChanged(nameof(SelectedMatchDate));
        RaisePropertyChanged(nameof(SelectedMatchStage));
        RaisePropertyChanged(nameof(SelectedMatchHomeTeam));
        RaisePropertyChanged(nameof(SelectedMatchAwayTeam));
        RaisePropertyChanged(nameof(SelectedMatchScore));
        RaisePropertyChanged(nameof(SelectedMatchGoals));
        RaisePropertyChanged(nameof(SelectedMatchAssists));
        RaisePropertyChanged(nameof(SelectedMatchShots));
        RaisePropertyChanged(nameof(SelectedMatchPasses));
        RaisePropertyChanged(nameof(MatchStatistics));
    }

    private int GetGoalTotal()
    {
        if (SelectedMatch is null)
        {
            return 0;
        }

        return Math.Max(0, SelectedMatch.HomeTeamGoal ?? 0) + Math.Max(0, SelectedMatch.AwayTeamGoal ?? 0);
    }

    private static string FormatScore(int? value) => value.HasValue ? value.Value.ToString() : "--";

    private IReadOnlyList<MatchStatisticViewModel> BuildMatchStatistics()
    {
        var goals = SelectedMatchGoals;
        var assists = SelectedMatchAssists;
        var shots = SelectedMatchShots;
        var passes = SelectedMatchPasses;
        var maxValue = Math.Max(1, new[] { goals, assists, shots, passes }.Max());

        return new[]
        {
            new MatchStatisticViewModel("Goals", goals, ScaleBarWidth(goals, maxValue), "#22c55e"),
            new MatchStatisticViewModel("Assists", assists, ScaleBarWidth(assists, maxValue), "#38bdf8"),
            new MatchStatisticViewModel("Shots", shots, ScaleBarWidth(shots, maxValue), "#22c55e"),
            new MatchStatisticViewModel("Passes", passes, ScaleBarWidth(passes, maxValue), "#16a34a"),
        };
    }

    private static double ScaleBarWidth(int value, int maxValue)
    {
        if (maxValue <= 0)
        {
            return 4;
        }

        var ratio = Math.Clamp(value / (double)maxValue, 0.0, 1.0);
        return Math.Max(4, Math.Round(ratio * 100, 1));
    }

    public sealed record MatchStatisticViewModel(string Label, int Value, double BarWidth, string ColorHex);
}
