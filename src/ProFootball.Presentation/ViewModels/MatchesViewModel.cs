using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.League.Dtos;
using ProFootball.Application.League.Queries;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Application.Team.Dtos;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class MatchesViewModel : ObservableObject
{
    private const string AllSeasonsOption = "All Seasons";
    private static readonly TeamFilterOption AllTeamsOption = new(null, "All Teams");

    private readonly IQueryDispatcher _queryDispatcher;
    private readonly Action<int> _openDetails;
    private MatchListItemDto? _selectedMatch;
    private LeagueDto? _selectedLeague;
    private TeamFilterOption? _selectedTeamOption = AllTeamsOption;
    private string? _season;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private int _page = 1;
    private int _pageSize = 20;
    private int _totalCount;
    private bool _isUpdatingSeasonOptions;
    private bool _isUpdatingTeamOptions;

    public MatchesViewModel(
        IQueryDispatcher queryDispatcher,
        Action<int> openDetails)
    {
        _queryDispatcher = queryDispatcher;
        _openDetails = openDetails;

        Matches = new ObservableCollection<MatchListItemDto>();
        Leagues = new ObservableCollection<LeagueDto>();
        TeamOptions = new ObservableCollection<TeamFilterOption> { AllTeamsOption };
        SeasonOptions = new ObservableCollection<string> { AllSeasonsOption };
        _season = AllSeasonsOption;

        SearchCommand = new AsyncRelayCommand(StartSearchAsync, CommandExceptionHandler.Handle);
        LoadLeaguesCommand = new AsyncRelayCommand(LoadLeaguesAsync, CommandExceptionHandler.Handle);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, CommandExceptionHandler.Handle, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, CommandExceptionHandler.Handle, () => HasPreviousPage);
        OpenDetailsCommand = new RelayCommand(OpenDetails, () => SelectedMatch is not null);
    }

    public ObservableCollection<MatchListItemDto> Matches { get; }

    public ObservableCollection<LeagueDto> Leagues { get; }

    public ObservableCollection<TeamFilterOption> TeamOptions { get; }

    public ObservableCollection<string> SeasonOptions { get; }

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
            RaisePropertyChanged(nameof(HasSelectedMatch));
        }
    }

    public bool HasSelectedMatch => SelectedMatch is not null;

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

    public string SelectedMatchHomeScore => SelectedMatch is null
        ? "--"
        : FormatScore(SelectedMatch.HomeTeamGoal);

    public string SelectedMatchAwayScore => SelectedMatch is null
        ? "--"
        : FormatScore(SelectedMatch.AwayTeamGoal);

    public int SelectedMatchGoals => GetGoalTotal();

    public int SelectedMatchAssists => Math.Max(0, GetGoalTotal() - 1) + 1;

    public int SelectedMatchShots => SelectedMatch is null ? 0 : Math.Max(8, GetGoalTotal() * 8 + 10);

    public int SelectedMatchPasses => SelectedMatch is null ? 0 : Math.Max(120, GetGoalTotal() * 85 + 120);

    public IReadOnlyList<MatchStatisticViewModel> MatchStatistics => BuildMatchStatistics();

    public LeagueDto? SelectedLeague
    {
        get => _selectedLeague;
        set
        {
            if (!SetProperty(ref _selectedLeague, value))
            {
                return;
            }

            _ = OnLeagueChangedSafeAsync();
        }
    }

    public string? Season
    {
        get => _season;
        set
        {
            if (!SetProperty(ref _season, value))
            {
                return;
            }

            if (_isUpdatingSeasonOptions)
            {
                return;
            }

            _ = StartSearchSafeAsync();
        }
    }

    public TeamFilterOption? SelectedTeamOption
    {
        get => _selectedTeamOption;
        set
        {
            if (!SetProperty(ref _selectedTeamOption, value))
            {
                return;
            }

            if (_isUpdatingTeamOptions)
            {
                return;
            }

            _ = StartSearchSafeAsync();
        }
    }

    public DateTime? DateFrom
    {
        get => _dateFrom;
        set
        {
            var normalized = NormalizeUtcDate(value);
            if (!SetProperty(ref _dateFrom, normalized))
            {
                return;
            }

            _ = StartSearchSafeAsync();
        }
    }

    public DateTime? DateTo
    {
        get => _dateTo;
        set
        {
            var normalized = NormalizeUtcDate(value);
            if (!SetProperty(ref _dateTo, normalized))
            {
                return;
            }

            _ = StartSearchSafeAsync();
        }
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

    private async Task StartSearchSafeAsync()
    {
        try
        {
            await StartSearchAsync();
        }
        catch (Exception exception)
        {
            CommandExceptionHandler.Handle(exception);
        }
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

    public async Task LoadTeamOptionsAsync()
    {
        var selectedTeamApiId = SelectedTeamOption?.TeamApiId;
        var teams = await _queryDispatcher.DispatchAsync<GetMatchTeamsQuery, IReadOnlyList<TeamListItemDto>>(
            new GetMatchTeamsQuery(SelectedLeague?.Id));

        _isUpdatingTeamOptions = true;
        try
        {
            TeamOptions.Clear();
            TeamOptions.Add(AllTeamsOption);

            foreach (var team in teams)
            {
                TeamOptions.Add(new TeamFilterOption(team.TeamApiId, team.LongName));
            }

            SelectedTeamOption = selectedTeamApiId.HasValue
                ? TeamOptions.FirstOrDefault(option => option.TeamApiId == selectedTeamApiId.Value) ?? AllTeamsOption
                : AllTeamsOption;
        }
        finally
        {
            _isUpdatingTeamOptions = false;
        }
    }

    public async Task SearchAsync()
    {
        var result = await _queryDispatcher.DispatchAsync<MatchSearchQuery, PagedResult<MatchListItemDto>>(new MatchSearchQuery(
            SelectedLeague?.Id,
            NormalizeSeasonFilter(Season),
            SelectedTeamOption?.TeamApiId,
            NormalizeUtcDate(DateFrom),
            NormalizeUtcDate(DateTo),
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

        var selectedMatchId = SelectedMatch?.MatchApiId;
        SelectedMatch = selectedMatchId.HasValue
            ? Matches.FirstOrDefault(item => item.MatchApiId == selectedMatchId.Value)
            : null;

        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
    }

    private async Task OnLeagueChangedSafeAsync()
    {
        try
        {
            await LoadSeasonOptionsAsync();
            await LoadTeamOptionsAsync();
            await StartSearchAsync();
        }
        catch (Exception exception)
        {
            CommandExceptionHandler.Handle(exception);
        }
    }

    public async Task LoadSeasonOptionsAsync()
    {
        var selectedSeason = Season;
        var bySeason = await _queryDispatcher.DispatchAsync<GetMatchesBySeasonQuery, IReadOnlyList<MatchesBySeasonDto>>(
            new GetMatchesBySeasonQuery(SelectedLeague?.Id));
        var seasons = bySeason
            .Select(item => item.Season)
            .Where(season => !string.IsNullOrWhiteSpace(season))
            .Select(season => season.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(season => season, StringComparer.Ordinal)
            .ToList();

        _isUpdatingSeasonOptions = true;
        try
        {
            SeasonOptions.Clear();
            SeasonOptions.Add(AllSeasonsOption);
            foreach (var season in seasons)
            {
                SeasonOptions.Add(season);
            }

            if (!string.IsNullOrWhiteSpace(selectedSeason) && SeasonOptions.Contains(selectedSeason))
            {
                Season = selectedSeason;
                return;
            }

            Season = AllSeasonsOption;
        }
        finally
        {
            _isUpdatingSeasonOptions = false;
        }
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
        RaisePropertyChanged(nameof(SelectedMatchHomeScore));
        RaisePropertyChanged(nameof(SelectedMatchAwayScore));
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

    private static string? NormalizeSeasonFilter(string? season)
    {
        if (string.IsNullOrWhiteSpace(season))
        {
            return null;
        }

        return season.Equals(AllSeasonsOption, StringComparison.OrdinalIgnoreCase)
            ? null
            : season.Trim();
    }

    private static DateTime? NormalizeUtcDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc);
    }

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

    public sealed record TeamFilterOption(int? TeamApiId, string Name);

    public sealed record MatchStatisticViewModel(string Label, int Value, double BarWidth, string ColorHex);
}
