using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class TeamsViewModel : ObservableObject, IDisposable
{
    private readonly ITeamsQueryService _teamsQueryService;
    private readonly Action<int> _openDetails;
    private readonly DispatcherTimer _nameSearchDebounceTimer;
    private TeamListItemDto? _selectedTeam;
    private TeamDetailsDto? _selectedTeamDetails;
    private string? _nameFilter;
    private bool _isLoadingList;
    private bool _isLoadingDetails;
    private string? _errorMessage;
    private int _page = 1;
    private int _pageSize = 30;
    private int _totalCount;
    private int _buildUpPlayScore;
    private int _chanceCreationScore;
    private int _defenceScore;
    private int _defenceAggressionScore;
    private string _teamTrendPolylinePoints = string.Empty;
    private long _searchVersion;
    private long _detailsVersion;

    public TeamsViewModel(ITeamsQueryService teamsQueryService, Action<int> openDetails)
    {
        _teamsQueryService = teamsQueryService;
        _openDetails = openDetails;

        _nameSearchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300),
        };
        _nameSearchDebounceTimer.Tick += OnNameSearchDebounceTimerTick;

        Teams = new ObservableCollection<TeamListItemDto>();
        TacticalMetrics = new ObservableCollection<TeamMetricEntryViewModel>();

        SearchCommand = new AsyncRelayCommand(StartSearchAsync, CommandExceptionHandler.Handle);
        OpenDetailsCommand = new RelayCommand(OpenDetails, () => SelectedTeam is not null);
    }

    public ObservableCollection<TeamListItemDto> Teams { get; }

    public ObservableCollection<TeamMetricEntryViewModel> TacticalMetrics { get; }

    public TeamListItemDto? SelectedTeam
    {
        get => _selectedTeam;
        set
        {
            if (!SetProperty(ref _selectedTeam, value))
            {
                return;
            }

            OpenDetailsCommand.RaiseCanExecuteChanged();
            RaiseSelectedTeamPropertiesChanged();
            _ = LoadSelectedTeamDetailsSafeAsync();
        }
    }

    public string? NameFilter
    {
        get => _nameFilter;
        set
        {
            if (!SetProperty(ref _nameFilter, value))
            {
                return;
            }

            ScheduleDebouncedSearch();
        }
    }

    public bool IsLoadingList
    {
        get => _isLoadingList;
        private set => SetProperty(ref _isLoadingList, value);
    }

    public bool IsLoadingDetails
    {
        get => _isLoadingDetails;
        private set => SetProperty(ref _isLoadingDetails, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public int Page
    {
        get => _page;
        private set => SetProperty(ref _page, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                _ = StartSearchSafeAsync();
            }
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    public string SelectedTeamName => _selectedTeamDetails?.Team.LongName ?? SelectedTeam?.LongName ?? "Select team";

    public string SelectedTeamShortName => NormalizeText(_selectedTeamDetails?.Team.ShortName ?? SelectedTeam?.ShortName, "--") ?? "--";

    public int BuildUpPlayScore
    {
        get => _buildUpPlayScore;
        private set => SetProperty(ref _buildUpPlayScore, value);
    }

    public int ChanceCreationScore
    {
        get => _chanceCreationScore;
        private set => SetProperty(ref _chanceCreationScore, value);
    }

    public int DefenceScore
    {
        get => _defenceScore;
        private set => SetProperty(ref _defenceScore, value);
    }

    public int DefenceAggressionScore
    {
        get => _defenceAggressionScore;
        private set => SetProperty(ref _defenceAggressionScore, value);
    }

    public string TeamTrendPolylinePoints
    {
        get => _teamTrendPolylinePoints;
        private set => SetProperty(ref _teamTrendPolylinePoints, value);
    }

    public AsyncRelayCommand SearchCommand { get; }

    public RelayCommand OpenDetailsCommand { get; }

    public async Task SearchAsync()
    {
        var searchVersion = Interlocked.Increment(ref _searchVersion);
        IsLoadingList = true;
        ErrorMessage = null;

        try
        {
            var result = await _teamsQueryService.SearchTeamsAsync(new TeamSearchQuery(
                Name: NormalizeText(NameFilter, null),
                SortBy: null,
                SortDescending: false,
                Page: Page,
                PageSize: Math.Max(10, PageSize)));

            if (searchVersion != Volatile.Read(ref _searchVersion))
            {
                return;
            }

            TotalCount = result.TotalCount;
            var selectedTeamApiId = SelectedTeam?.TeamApiId;

            Teams.Clear();
            foreach (var item in result.Items)
            {
                Teams.Add(item);
            }

            SelectedTeam = selectedTeamApiId.HasValue
                ? Teams.FirstOrDefault(item => item.TeamApiId == selectedTeamApiId.Value) ?? Teams.FirstOrDefault()
                : Teams.FirstOrDefault();
        }
        catch (Exception exception)
        {
            if (searchVersion == Volatile.Read(ref _searchVersion))
            {
                ErrorMessage = "Failed to load teams.";
            }

            CommandExceptionHandler.Handle(exception);
        }
        finally
        {
            if (searchVersion == Volatile.Read(ref _searchVersion))
            {
                IsLoadingList = false;
            }
        }
    }

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

    private async Task LoadSelectedTeamDetailsSafeAsync()
    {
        var selectedTeam = SelectedTeam;
        var detailsVersion = Interlocked.Increment(ref _detailsVersion);

        if (selectedTeam is null)
        {
            _selectedTeamDetails = null;
            TacticalMetrics.Clear();
            BuildUpPlayScore = 0;
            ChanceCreationScore = 0;
            DefenceScore = 0;
            DefenceAggressionScore = 0;
            TeamTrendPolylinePoints = string.Empty;
            RaiseSelectedTeamPropertiesChanged();
            return;
        }

        IsLoadingDetails = true;
        ErrorMessage = null;

        try
        {
            var details = await _teamsQueryService.GetTeamDetailsAsync(selectedTeam.TeamApiId);
            if (detailsVersion != Volatile.Read(ref _detailsVersion))
            {
                return;
            }

            _selectedTeamDetails = details;
            var latest = details?.Attributes.FirstOrDefault();

            var buildUpPlaySpeed = latest?.BuildUpPlaySpeed ?? ResolveFallbackMetric(selectedTeam.TeamApiId, 74);
            var buildUpPlayPassing = latest?.BuildUpPlayPassing ?? ResolveFallbackMetric(selectedTeam.TeamApiId, 77);
            var chanceCreationPassing = latest?.ChanceCreationPassing ?? ResolveFallbackMetric(selectedTeam.TeamApiId, 75);
            var defencePressure = latest?.DefencePressure ?? ResolveFallbackMetric(selectedTeam.TeamApiId, 72);
            var defenceAggression = defencePressure;

            BuildUpPlayScore = (buildUpPlaySpeed + buildUpPlayPassing) / 2;
            ChanceCreationScore = chanceCreationPassing;
            DefenceScore = defencePressure;
            DefenceAggressionScore = defenceAggression;

            TacticalMetrics.Clear();
            TacticalMetrics.Add(new TeamMetricEntryViewModel("Build Up Play Speed", buildUpPlaySpeed));
            TacticalMetrics.Add(new TeamMetricEntryViewModel("Build Up Passing", buildUpPlayPassing));
            TacticalMetrics.Add(new TeamMetricEntryViewModel("Chance Creation", chanceCreationPassing));
            TacticalMetrics.Add(new TeamMetricEntryViewModel("Defence Pressure", defencePressure));
            TacticalMetrics.Add(new TeamMetricEntryViewModel("Defence Aggression", defenceAggression));

            UpdateTrendPolyline(details?.Attributes);
            RaiseSelectedTeamPropertiesChanged();
        }
        catch (Exception exception)
        {
            if (detailsVersion == Volatile.Read(ref _detailsVersion))
            {
                ErrorMessage = "Failed to load team profile.";
            }

            CommandExceptionHandler.Handle(exception);
        }
        finally
        {
            if (detailsVersion == Volatile.Read(ref _detailsVersion))
            {
                IsLoadingDetails = false;
            }
        }
    }

    private void UpdateTrendPolyline(IReadOnlyList<TeamAttributeDto>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            TeamTrendPolylinePoints = string.Empty;
            return;
        }

        var trendValues = attributes
            .OrderBy(attribute => attribute.Date)
            .ThenBy(attribute => attribute.DefencePressure)
            .TakeLast(8)
            .Select(attribute =>
            {
                var speed = attribute.BuildUpPlaySpeed ?? 72;
                var passing = attribute.BuildUpPlayPassing ?? 72;
                var chance = attribute.ChanceCreationPassing ?? 72;
                var defence = attribute.DefencePressure ?? 72;
                return (speed + passing + chance + defence) / 4.0;
            })
            .ToList();

        if (trendValues.Count == 0)
        {
            TeamTrendPolylinePoints = string.Empty;
            return;
        }

        const double left = 40;
        const double right = 720;
        const double top = 26;
        const double bottom = 196;

        var points = trendValues
            .Select((value, index) =>
            {
                var horizontalRatio = trendValues.Count == 1 ? 0.5 : (double)index / (trendValues.Count - 1);
                var x = left + (right - left) * horizontalRatio;
                var y = bottom - Math.Clamp(value, 0, 100) / 100.0 * (bottom - top);
                return string.Create(CultureInfo.InvariantCulture, $"{x:F1},{y:F1}");
            });

        TeamTrendPolylinePoints = string.Join(" ", points);
    }

    private void OnNameSearchDebounceTimerTick(object? sender, EventArgs e)
    {
        _nameSearchDebounceTimer.Stop();
        _ = StartSearchSafeAsync();
    }

    private void ScheduleDebouncedSearch()
    {
        _nameSearchDebounceTimer.Stop();
        _nameSearchDebounceTimer.Start();
    }

    private void OpenDetails()
    {
        if (SelectedTeam is null)
        {
            return;
        }

        _openDetails(SelectedTeam.TeamApiId);
    }

    private void RaiseSelectedTeamPropertiesChanged()
    {
        RaisePropertyChanged(nameof(SelectedTeamName));
        RaisePropertyChanged(nameof(SelectedTeamShortName));
    }

    private static int ResolveFallbackMetric(int teamApiId, int baseline)
    {
        var offset = ((Math.Abs(teamApiId) * 13) % 11) - 5;
        return Math.Clamp(baseline + offset, 45, 95);
    }

    private static string? NormalizeText(string? value, string? defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim();
    }

    public void Dispose()
    {
        _nameSearchDebounceTimer.Stop();
        _nameSearchDebounceTimer.Tick -= OnNameSearchDebounceTimerTick;
    }
}

public sealed record TeamMetricEntryViewModel(string Name, int Value);
