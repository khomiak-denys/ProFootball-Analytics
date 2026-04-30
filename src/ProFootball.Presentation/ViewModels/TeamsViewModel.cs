using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Team.Commands;
using ProFootball.Application.League.Dtos;
using ProFootball.Application.League.Queries;
using ProFootball.Application.Team.Dtos;
using ProFootball.Application.Team.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class TeamsViewModel : ObservableObject, IDisposable
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly Action<int> _openDetails;
    private readonly DispatcherTimer _nameSearchDebounceTimer;
    private TeamListItemDto? _selectedTeam;
    private TeamDetailsDto? _selectedTeamDetails;
    private string? _nameFilter;
    private bool _isLoadingList;
    private bool _isLoadingDetails;
    private string? _errorMessage;
    private int _page = 1;
    private int _pageSize = 20;
    private int _totalCount;
    private int _buildUpPlayScore;
    private int _chanceCreationScore;
    private int _defenceScore;
    private string _teamTrendPolylinePoints = string.Empty;
    private long _searchVersion;
    private long _detailsVersion;
    private bool _isAddTeamModalOpen;
    private string _newTeamName = string.Empty;
    private string _newTeamShortName = string.Empty;
    private LeagueDto? _selectedLeague;
    public TeamsViewModel(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher, Action<int> openDetails)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _openDetails = openDetails;

        _nameSearchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300),
        };
        _nameSearchDebounceTimer.Tick += OnNameSearchDebounceTimerTick;

        Teams = new ObservableCollection<TeamListItemDto>();
        TacticalMetrics = new ObservableCollection<TeamMetricEntryViewModel>();
        Leagues = new ObservableCollection<LeagueDto>();

        SearchCommand = new AsyncRelayCommand(StartSearchAsync, CommandExceptionHandler.Handle);
        OpenDetailsCommand = new RelayCommand(OpenDetails, () => SelectedTeam is not null);
        OpenAddTeamModalCommand = new RelayCommand(OpenAddTeamModal);
        CloseAddTeamModalCommand = new RelayCommand(CloseAddTeamModal);
        AddTeamCommand = new AsyncRelayCommand(AddTeamAsync, CommandExceptionHandler.Handle);
    }

    public ObservableCollection<TeamListItemDto> Teams { get; }
    public ObservableCollection<LeagueDto> Leagues { get; }

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

    public string TeamTrendPolylinePoints
    {
        get => _teamTrendPolylinePoints;
        private set => SetProperty(ref _teamTrendPolylinePoints, value);
    }

    public AsyncRelayCommand SearchCommand { get; }

    public RelayCommand OpenDetailsCommand { get; }

    public RelayCommand OpenAddTeamModalCommand { get; }

    public RelayCommand CloseAddTeamModalCommand { get; }

    public AsyncRelayCommand AddTeamCommand { get; }

    public bool IsAddTeamModalOpen
    {
        get => _isAddTeamModalOpen;
        private set => SetProperty(ref _isAddTeamModalOpen, value);
    }

    public string NewTeamName
    {
        get => _newTeamName;
        set => SetProperty(ref _newTeamName, value);
    }

    public string NewTeamShortName
    {
        get => _newTeamShortName;
        set => SetProperty(ref _newTeamShortName, value);
    }

    public LeagueDto? SelectedLeague
    {
        get => _selectedLeague;
        set => SetProperty(ref _selectedLeague, value);
    }

    public async Task SearchAsync()
    {
        var searchVersion = Interlocked.Increment(ref _searchVersion);
        IsLoadingList = true;
        ErrorMessage = null;

        try
        {
            await EnsureLeaguesLoadedAsync();

            var result = await _queryDispatcher.DispatchAsync<TeamSearchQuery, PagedResult<TeamListItemDto>>(new TeamSearchQuery(
                Name: NormalizeText(NameFilter, null),
                SortBy: null,
                SortDescending: false,
                Page: Page,
                PageSize: Math.Max(20, PageSize)));

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
            ClearSelectedTeamDetailsState();
            return;
        }

        IsLoadingDetails = true;
        ErrorMessage = null;
        ClearSelectedTeamDetailsState();

        try
        {
            var details = await _queryDispatcher.DispatchAsync<GetTeamDetailsQuery, TeamDetailsDto?>(
                new GetTeamDetailsQuery(selectedTeam.TeamApiId));
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

            BuildUpPlayScore = (buildUpPlaySpeed + buildUpPlayPassing) / 2;
            ChanceCreationScore = chanceCreationPassing;
            DefenceScore = defencePressure;

            TacticalMetrics.Clear();
            TacticalMetrics.Add(new TeamMetricEntryViewModel("Build Up Play Speed", buildUpPlaySpeed));
            TacticalMetrics.Add(new TeamMetricEntryViewModel("Build Up Passing", buildUpPlayPassing));
            TacticalMetrics.Add(new TeamMetricEntryViewModel("Chance Creation", chanceCreationPassing));
            TacticalMetrics.Add(new TeamMetricEntryViewModel("Defence Pressure", defencePressure));

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

    private void ClearSelectedTeamDetailsState()
    {
        _selectedTeamDetails = null;
        TacticalMetrics.Clear();
        BuildUpPlayScore = 0;
        ChanceCreationScore = 0;
        DefenceScore = 0;
        TeamTrendPolylinePoints = string.Empty;
        RaiseSelectedTeamPropertiesChanged();
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

    private async Task EnsureLeaguesLoadedAsync()
    {
        if (Leagues.Count > 0)
        {
            return;
        }

        await ReloadLeaguesAsync();
    }

    private async Task ReloadLeaguesAsync()
    {
        var leagues = await _queryDispatcher.DispatchAsync<GetLeaguesQuery, IReadOnlyList<LeagueDto>>(new GetLeaguesQuery());
        Leagues.Clear();
        foreach (var league in leagues)
        {
            Leagues.Add(league);
        }
    }

    private void OpenAddTeamModal()
    {
        _ = OpenAddTeamModalSafeAsync();
    }

    private async Task OpenAddTeamModalSafeAsync()
    {
        try
        {
            await ReloadLeaguesAsync();
            ErrorMessage = null;
            NewTeamName = string.Empty;
            NewTeamShortName = string.Empty;
            SelectedLeague = Leagues.FirstOrDefault();
            IsAddTeamModalOpen = true;
        }
        catch (Exception exception)
        {
            ErrorMessage = "Failed to load leagues.";
            CommandExceptionHandler.Handle(exception);
        }
    }

    private void CloseAddTeamModal()
    {
        IsAddTeamModalOpen = false;
    }

    private async Task AddTeamAsync()
    {
        await EnsureLeaguesLoadedAsync();

        if (string.IsNullOrWhiteSpace(NewTeamName))
        {
            ErrorMessage = "Team name is required.";
            return;
        }

        if (SelectedLeague is null)
        {
            ErrorMessage = "League is required.";
            return;
        }

        var normalizedShortName = NormalizeText(NewTeamShortName, null);
        var shortName = string.IsNullOrWhiteSpace(normalizedShortName)
            ? BuildShortCode(NewTeamName)
            : normalizedShortName.ToUpperInvariant();
        var result = await _commandDispatcher.DispatchAsync<CreateTeamCommand, Result>(
            new CreateTeamCommand(NewTeamName.Trim(), shortName));
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        await StartSearchAsync();
        ErrorMessage = null;
        IsAddTeamModalOpen = false;
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

    private static string BuildShortCode(string name)
    {
        var letters = name.Where(char.IsLetter).Take(3).ToArray();
        if (letters.Length == 0)
        {
            return "NEW";
        }

        return new string(letters).ToUpperInvariant();
    }

    public void Dispose()
    {
        _nameSearchDebounceTimer.Stop();
        _nameSearchDebounceTimer.Tick -= OnNameSearchDebounceTimerTick;
    }
}

public sealed record TeamMetricEntryViewModel(string Name, int Value);
