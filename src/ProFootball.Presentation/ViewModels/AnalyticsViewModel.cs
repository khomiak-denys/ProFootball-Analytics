using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Application.Player.Dtos;
using ProFootball.Application.Player.Queries;
using ProFootball.Application.Team.Dtos;
using ProFootball.Application.Team.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class AnalyticsViewModel : ObservableObject, IDisposable
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly IReadOnlyList<string> _metricOptions;
    private readonly IReadOnlyList<string> _teamMetricOptions;
    private IReadOnlyList<TopPlayerDto> _playersSnapshot = Array.Empty<TopPlayerDto>();
    private IReadOnlyList<TeamListItemDto> _teamsSnapshot = Array.Empty<TeamListItemDto>();

    private AnalyticsCompareMode _compareMode = AnalyticsCompareMode.Players;
    private AnalyticsPlayerOptionViewModel? _selectedPlayer1;
    private AnalyticsPlayerOptionViewModel? _selectedPlayer2;
    private AnalyticsTeamOptionViewModel? _selectedTeam1;
    private AnalyticsTeamOptionViewModel? _selectedTeam2;
    private string _selectedMetric;
    private string _selectedTeamMetric;
    private bool _isLoading;
    private string? _errorMessage;
    private double _averageOverallRating;
    private double _averagePotential;
    private int _topPerformersCount;
    private int _risingStarsCount;
    private string _averageOverallDelta = "+0.0 from last month";
    private string _averagePotentialDelta = "+0.0 from last month";
    private PointCollection _radarPlayerOnePoints = new();
    private PointCollection _radarPlayerTwoPoints = new();
    private PointCollection _performanceTrendPoints = new();
    private double _trendAxisMin = 75;
    private double _trendAxisMidLow = 79;
    private double _trendAxisMidHigh = 83;
    private double _trendAxisMax = 90;
    private CancellationTokenSource? _refreshAllCts;
    private CancellationTokenSource? _visualRefreshCts;
    private long _visualStateVersion;
    private bool _isUpdatingSelection;
    private string _selectedTeamOneName = "Team 1";
    private string _selectedTeamTwoName = "Team 2";
    private double _selectedTeamOneMetricValue;
    private double _selectedTeamTwoMetricValue;
    private string _teamsModeSummary = "--";

    public AnalyticsViewModel(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
        _metricOptions = ["Overall Rating", "Potential"];
        _teamMetricOptions = ["Build Up Speed", "Build Up Passing", "Chance Creation", "Defence Pressure"];
        _selectedMetric = _metricOptions[0];
        _selectedTeamMetric = _teamMetricOptions[0];

        AvailablePlayers = new ObservableCollection<AnalyticsPlayerOptionViewModel>();
        AvailableTeams = new ObservableCollection<AnalyticsTeamOptionViewModel>();
        TrendPoints = new ObservableCollection<AnalyticsTrendPointViewModel>();
        TopPerformerBars = new ObservableCollection<AnalyticsTopPerformerBarViewModel>();
        GoalsAssistsBars = new ObservableCollection<AnalyticsGoalsAssistsBarViewModel>();
        TopPerformerCards = new ObservableCollection<AnalyticsTopPerformerCardViewModel>();

        RefreshAllCommand = new AsyncRelayCommand(RefreshAllAsync, CommandExceptionHandler.Handle);
        ComparePlayersModeCommand = new RelayCommand(() => CompareMode = AnalyticsCompareMode.Players);
        CompareTeamsModeCommand = new RelayCommand(() => CompareMode = AnalyticsCompareMode.Teams);
    }

    public ObservableCollection<AnalyticsPlayerOptionViewModel> AvailablePlayers { get; }

    public ObservableCollection<AnalyticsTeamOptionViewModel> AvailableTeams { get; }

    public ObservableCollection<AnalyticsTrendPointViewModel> TrendPoints { get; }

    public ObservableCollection<AnalyticsTopPerformerBarViewModel> TopPerformerBars { get; }

    public ObservableCollection<AnalyticsGoalsAssistsBarViewModel> GoalsAssistsBars { get; }

    public ObservableCollection<AnalyticsTopPerformerCardViewModel> TopPerformerCards { get; }

    public IReadOnlyList<string> MetricOptions => _metricOptions;

    public IReadOnlyList<string> TeamMetricOptions => _teamMetricOptions;

    public AnalyticsCompareMode CompareMode
    {
        get => _compareMode;
        set
        {
            if (SetProperty(ref _compareMode, value))
            {
                RaisePropertyChanged(nameof(IsComparePlayersMode));
                RaisePropertyChanged(nameof(IsCompareTeamsMode));
                _ = RefreshVisualStateSafeAsync();
            }
        }
    }

    public bool IsComparePlayersMode => CompareMode == AnalyticsCompareMode.Players;

    public bool IsCompareTeamsMode => CompareMode == AnalyticsCompareMode.Teams;

    public AnalyticsPlayerOptionViewModel? SelectedPlayer1
    {
        get => _selectedPlayer1;
        set
        {
            if (!SetProperty(ref _selectedPlayer1, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(PlayerOneLegendLabel));

            if (_isUpdatingSelection)
            {
                return;
            }

            _ = RefreshVisualStateSafeAsync();
        }
    }

    public AnalyticsPlayerOptionViewModel? SelectedPlayer2
    {
        get => _selectedPlayer2;
        set
        {
            if (!SetProperty(ref _selectedPlayer2, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(PlayerTwoLegendLabel));

            if (_isUpdatingSelection)
            {
                return;
            }

            _ = RefreshVisualStateSafeAsync();
        }
    }

    public AnalyticsTeamOptionViewModel? SelectedTeam1
    {
        get => _selectedTeam1;
        set
        {
            if (!SetProperty(ref _selectedTeam1, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(SelectedTeamOneName));

            if (_isUpdatingSelection || !IsCompareTeamsMode)
            {
                return;
            }

            _ = RefreshVisualStateSafeAsync();
        }
    }

    public AnalyticsTeamOptionViewModel? SelectedTeam2
    {
        get => _selectedTeam2;
        set
        {
            if (!SetProperty(ref _selectedTeam2, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(SelectedTeamTwoName));

            if (_isUpdatingSelection || !IsCompareTeamsMode)
            {
                return;
            }

            _ = RefreshVisualStateSafeAsync();
        }
    }

    public string SelectedMetric
    {
        get => _selectedMetric;
        set
        {
            if (!SetProperty(ref _selectedMetric, value))
            {
                return;
            }

            _ = RefreshVisualStateSafeAsync();
        }
    }

    public string SelectedTeamMetric
    {
        get => _selectedTeamMetric;
        set
        {
            if (!SetProperty(ref _selectedTeamMetric, value))
            {
                return;
            }

            if (!IsCompareTeamsMode)
            {
                return;
            }

            _ = RefreshVisualStateSafeAsync();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public double AverageOverallRating
    {
        get => _averageOverallRating;
        private set => SetProperty(ref _averageOverallRating, value);
    }

    public double AveragePotential
    {
        get => _averagePotential;
        private set => SetProperty(ref _averagePotential, value);
    }

    public int TopPerformersCount
    {
        get => _topPerformersCount;
        private set => SetProperty(ref _topPerformersCount, value);
    }

    public int RisingStarsCount
    {
        get => _risingStarsCount;
        private set => SetProperty(ref _risingStarsCount, value);
    }

    public string AverageOverallDelta
    {
        get => _averageOverallDelta;
        private set => SetProperty(ref _averageOverallDelta, value);
    }

    public string AveragePotentialDelta
    {
        get => _averagePotentialDelta;
        private set => SetProperty(ref _averagePotentialDelta, value);
    }

    public PointCollection RadarPlayerOnePoints
    {
        get => _radarPlayerOnePoints;
        private set => SetProperty(ref _radarPlayerOnePoints, value);
    }

    public PointCollection RadarPlayerTwoPoints
    {
        get => _radarPlayerTwoPoints;
        private set => SetProperty(ref _radarPlayerTwoPoints, value);
    }

    public PointCollection PerformanceTrendPoints
    {
        get => _performanceTrendPoints;
        private set => SetProperty(ref _performanceTrendPoints, value);
    }

    public double TrendAxisMin
    {
        get => _trendAxisMin;
        private set => SetProperty(ref _trendAxisMin, value);
    }

    public double TrendAxisMidLow
    {
        get => _trendAxisMidLow;
        private set => SetProperty(ref _trendAxisMidLow, value);
    }

    public double TrendAxisMidHigh
    {
        get => _trendAxisMidHigh;
        private set => SetProperty(ref _trendAxisMidHigh, value);
    }

    public double TrendAxisMax
    {
        get => _trendAxisMax;
        private set => SetProperty(ref _trendAxisMax, value);
    }

    public string PlayerOneLegendLabel => SelectedPlayer1?.PlayerName ?? "Player 1";

    public string PlayerTwoLegendLabel => SelectedPlayer2?.PlayerName ?? "Player 2";

    public string SelectedTeamOneName => _selectedTeamOneName;

    public string SelectedTeamTwoName => _selectedTeamTwoName;

    public double SelectedTeamOneMetricValue
    {
        get => _selectedTeamOneMetricValue;
        private set => SetProperty(ref _selectedTeamOneMetricValue, value);
    }

    public double SelectedTeamTwoMetricValue
    {
        get => _selectedTeamTwoMetricValue;
        private set => SetProperty(ref _selectedTeamTwoMetricValue, value);
    }

    public string TeamsModeSummary
    {
        get => _teamsModeSummary;
        private set => SetProperty(ref _teamsModeSummary, value);
    }

    public AsyncRelayCommand RefreshAllCommand { get; }

    public RelayCommand ComparePlayersModeCommand { get; }

    public RelayCommand CompareTeamsModeCommand { get; }

    public async Task RefreshAllAsync()
    {
        var version = Interlocked.Increment(ref _visualStateVersion);
        var (refreshSource, previousRefreshSource) = ReplaceRefreshAllSource();
        var refreshToken = refreshSource.Token;
        var (visualSource, previousVisualSource) = ReplaceVisualRefreshSource();
        var visualToken = visualSource.Token;
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var topPlayersTask = _queryDispatcher.DispatchAsync<TopPlayersQuery, IReadOnlyList<TopPlayerDto>>(new TopPlayersQuery(
                Limit: 80,
                MinOverallRating: null,
                MinPotential: null,
                PreferredFoot: null,
                SortBy: "overall",
                SortDescending: true), refreshToken);
            var teamsTask = _queryDispatcher.DispatchAsync<TeamSearchQuery, PagedResult<TeamListItemDto>>(new TeamSearchQuery(
                Name: null,
                SortBy: null,
                SortDescending: false,
                Page: 1,
                PageSize: 120), refreshToken);

            await Task.WhenAll(topPlayersTask, teamsTask);
            _playersSnapshot = await topPlayersTask;
            _teamsSnapshot = (await teamsTask).Items;

            UpdatePlayerOptions();
            EnsureSelectedPlayers();
            UpdateTeamOptions();
            EnsureSelectedTeams();

            refreshToken.ThrowIfCancellationRequested();
            await RefreshVisualStateAsync(version, visualToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (version == Volatile.Read(ref _visualStateVersion))
            {
                ErrorMessage = "Failed to load analytics data.";
                CommandExceptionHandler.Handle(exception);
            }
        }
        finally
        {
            if (ReferenceEquals(_refreshAllCts, refreshSource))
            {
                _refreshAllCts = null;
            }

            if (ReferenceEquals(_visualRefreshCts, visualSource))
            {
                _visualRefreshCts = null;
            }

            DisposeSource(previousRefreshSource);
            DisposeSource(refreshSource);
            DisposeSource(previousVisualSource);
            DisposeSource(visualSource);

            if (version == Volatile.Read(ref _visualStateVersion))
            {
                IsLoading = false;
            }
        }
    }

    private async Task RefreshVisualStateSafeAsync()
    {
        var version = Interlocked.Increment(ref _visualStateVersion);
        var (currentVisualSource, previousVisualSource) = ReplaceVisualRefreshSource();
        var visualToken = currentVisualSource.Token;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await RefreshVisualStateAsync(version, visualToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (version == Volatile.Read(ref _visualStateVersion))
            {
                ErrorMessage = "Failed to update analytics visualizations.";
                CommandExceptionHandler.Handle(exception);
            }
        }
        finally
        {
            if (ReferenceEquals(_visualRefreshCts, currentVisualSource))
            {
                _visualRefreshCts = null;
            }

            DisposeSource(previousVisualSource);
            DisposeSource(currentVisualSource);

            if (version == Volatile.Read(ref _visualStateVersion))
            {
                IsLoading = false;
            }
        }
    }

    private async Task RefreshVisualStateAsync(long version, CancellationToken cancellationToken)
    {
        if (IsCompareTeamsMode)
        {
            await RefreshTeamsVisualStateAsync(version, cancellationToken);
            return;
        }

        var orderedPlayers = OrderByMetric(_playersSnapshot, SelectedMetric)
            .Take(5)
            .ToList();

        var trendTask1 = SelectedPlayer1 is null
            ? Task.FromResult<IReadOnlyList<PlayerTrendPointDto>>(Array.Empty<PlayerTrendPointDto>())
            : _queryDispatcher.DispatchAsync<GetPlayerTrendQuery, IReadOnlyList<PlayerTrendPointDto>>(
                new GetPlayerTrendQuery(SelectedPlayer1.PlayerApiId),
                cancellationToken);
        var trendTask2 = SelectedPlayer2 is null
            ? Task.FromResult<IReadOnlyList<PlayerTrendPointDto>>(Array.Empty<PlayerTrendPointDto>())
            : _queryDispatcher.DispatchAsync<GetPlayerTrendQuery, IReadOnlyList<PlayerTrendPointDto>>(
                new GetPlayerTrendQuery(SelectedPlayer2.PlayerApiId),
                cancellationToken);
        await Task.WhenAll(trendTask1, trendTask2);

        var playerOneTrend = await trendTask1;
        var playerTwoTrend = await trendTask2;
        cancellationToken.ThrowIfCancellationRequested();

        if (version != Volatile.Read(ref _visualStateVersion))
        {
            return;
        }

        BuildKpiCards();
        BuildKpiDeltasFromTrend(playerOneTrend, playerTwoTrend);
        BuildTopPerformerBars(orderedPlayers);
        BuildGoalsAndAssists(orderedPlayers);
        BuildTopPerformerCards(orderedPlayers);
        BuildRadar(ResolvePlayerById(SelectedPlayer1?.PlayerApiId), ResolvePlayerById(SelectedPlayer2?.PlayerApiId));
        BuildTrend(playerOneTrend, playerTwoTrend);

    }

    private async Task RefreshTeamsVisualStateAsync(long version, CancellationToken cancellationToken)
    {
        var selectedTeamOne = SelectedTeam1;
        var selectedTeamTwo = SelectedTeam2;
        if (selectedTeamOne is null && selectedTeamTwo is null)
        {
            TopPerformerBars.Clear();
            GoalsAssistsBars.Clear();
            TopPerformerCards.Clear();
            TrendPoints.Clear();
            PerformanceTrendPoints = new PointCollection();
            RadarPlayerOnePoints = new PointCollection();
            RadarPlayerTwoPoints = new PointCollection();
            TeamsModeSummary = "No teams available for comparison.";
            return;
        }

        selectedTeamOne ??= selectedTeamTwo;
        selectedTeamTwo ??= selectedTeamOne;

        var teamOneTask = selectedTeamOne is null
            ? Task.FromResult<TeamDetailsDto?>(null)
            : _queryDispatcher.DispatchAsync<GetTeamDetailsQuery, TeamDetailsDto?>(
                new GetTeamDetailsQuery(selectedTeamOne.TeamApiId),
                cancellationToken);
        var teamTwoTask = selectedTeamTwo is null
            ? Task.FromResult<TeamDetailsDto?>(null)
            : _queryDispatcher.DispatchAsync<GetTeamDetailsQuery, TeamDetailsDto?>(
                new GetTeamDetailsQuery(selectedTeamTwo.TeamApiId),
                cancellationToken);

        await Task.WhenAll(teamOneTask, teamTwoTask);

        var teamOneDetails = await teamOneTask;
        var teamTwoDetails = await teamTwoTask;
        cancellationToken.ThrowIfCancellationRequested();
        if (version != Volatile.Read(ref _visualStateVersion))
        {
            return;
        }

        var teamOneMetric = ResolveTeamMetricValue(teamOneDetails?.Attributes.FirstOrDefault(), SelectedTeamMetric);
        var teamTwoMetric = ResolveTeamMetricValue(teamTwoDetails?.Attributes.FirstOrDefault(), SelectedTeamMetric);

        _selectedTeamOneName = teamOneDetails?.Team.LongName ?? selectedTeamOne?.TeamName ?? "Team 1";
        _selectedTeamTwoName = teamTwoDetails?.Team.LongName ?? selectedTeamTwo?.TeamName ?? "Team 2";
        RaisePropertyChanged(nameof(SelectedTeamOneName));
        RaisePropertyChanged(nameof(SelectedTeamTwoName));
        SelectedTeamOneMetricValue = Math.Round(teamOneMetric, 1);
        SelectedTeamTwoMetricValue = Math.Round(teamTwoMetric, 1);
        TeamsModeSummary = BuildTeamsSummary(SelectedTeamMetric, SelectedTeamOneName, SelectedTeamTwoName, teamOneMetric, teamTwoMetric);

        TopPerformersCount = teamOneDetails?.Attributes.Count ?? 0;
        RisingStarsCount = teamTwoDetails?.Attributes.Count ?? 0;
        AverageOverallRating = Math.Round((teamOneMetric + teamTwoMetric) / 2.0, 1);
        AveragePotential = Math.Round(Math.Abs(teamOneMetric - teamTwoMetric), 1);
        AverageOverallDelta = "based on latest attributes";
        AveragePotentialDelta = "team-to-team gap";

        BuildTeamsBars(SelectedTeamOneName, teamOneMetric, SelectedTeamTwoName, teamTwoMetric);
        GoalsAssistsBars.Clear();
        TopPerformerCards.Clear();
        RadarPlayerOnePoints = BuildTeamRadarPolygonPoints(teamOneDetails);
        RadarPlayerTwoPoints = BuildTeamRadarPolygonPoints(teamTwoDetails);
        BuildTeamTrend(teamOneDetails, teamTwoDetails, SelectedTeamMetric);
    }

    private (CancellationTokenSource Current, CancellationTokenSource? Previous) ReplaceRefreshAllSource()
    {
        var newSource = new CancellationTokenSource();
        var previousSource = Interlocked.Exchange(ref _refreshAllCts, newSource);
        CancelSource(previousSource);
        return (newSource, previousSource);
    }

    private (CancellationTokenSource Current, CancellationTokenSource? Previous) ReplaceVisualRefreshSource()
    {
        var newSource = new CancellationTokenSource();
        var previousSource = Interlocked.Exchange(ref _visualRefreshCts, newSource);
        CancelSource(previousSource);
        return (newSource, previousSource);
    }

    private static void CancelSource(CancellationTokenSource? source)
    {
        if (source is null)
        {
            return;
        }

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void DisposeSource(CancellationTokenSource? source)
    {
        if (source is null)
        {
            return;
        }

        try
        {
            source.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void UpdatePlayerOptions()
    {
        var selectedPlayerOneId = SelectedPlayer1?.PlayerApiId;
        var selectedPlayerTwoId = SelectedPlayer2?.PlayerApiId;

        AvailablePlayers.Clear();
        foreach (var player in _playersSnapshot.Take(40))
        {
            AvailablePlayers.Add(new AnalyticsPlayerOptionViewModel(
                player.PlayerApiId,
                player.PlayerName,
                player.AverageOverallRating,
                player.AveragePotential,
                player.Samples));
        }

        _isUpdatingSelection = true;
        try
        {
            SelectedPlayer1 = selectedPlayerOneId.HasValue
                ? AvailablePlayers.FirstOrDefault(player => player.PlayerApiId == selectedPlayerOneId.Value)
                : AvailablePlayers.FirstOrDefault();
            SelectedPlayer2 = selectedPlayerTwoId.HasValue
                ? AvailablePlayers.FirstOrDefault(player => player.PlayerApiId == selectedPlayerTwoId.Value)
                : AvailablePlayers.Skip(1).FirstOrDefault();

            if (SelectedPlayer2 is null)
            {
                SelectedPlayer2 = SelectedPlayer1;
            }
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void EnsureSelectedPlayers()
    {
        var resolvedPlayerOne = SelectedPlayer1 ?? AvailablePlayers.FirstOrDefault();
        var resolvedPlayerTwo = SelectedPlayer2;
        if (resolvedPlayerTwo is null || resolvedPlayerTwo.PlayerApiId == resolvedPlayerOne?.PlayerApiId)
        {
            resolvedPlayerTwo = AvailablePlayers.FirstOrDefault(player =>
                player.PlayerApiId != resolvedPlayerOne?.PlayerApiId) ?? resolvedPlayerOne;
        }

        _isUpdatingSelection = true;
        try
        {
            SelectedPlayer1 = resolvedPlayerOne;
            SelectedPlayer2 = resolvedPlayerTwo;
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void UpdateTeamOptions()
    {
        var selectedTeamOneId = SelectedTeam1?.TeamApiId;
        var selectedTeamTwoId = SelectedTeam2?.TeamApiId;

        AvailableTeams.Clear();
        foreach (var team in _teamsSnapshot.Take(80))
        {
            AvailableTeams.Add(new AnalyticsTeamOptionViewModel(team.TeamApiId, team.LongName));
        }

        _isUpdatingSelection = true;
        try
        {
            SelectedTeam1 = selectedTeamOneId.HasValue
                ? AvailableTeams.FirstOrDefault(team => team.TeamApiId == selectedTeamOneId.Value)
                : AvailableTeams.FirstOrDefault();
            SelectedTeam2 = selectedTeamTwoId.HasValue
                ? AvailableTeams.FirstOrDefault(team => team.TeamApiId == selectedTeamTwoId.Value)
                : AvailableTeams.Skip(1).FirstOrDefault();

            if (SelectedTeam2 is null)
            {
                SelectedTeam2 = SelectedTeam1;
            }
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void EnsureSelectedTeams()
    {
        var resolvedTeamOne = SelectedTeam1 ?? AvailableTeams.FirstOrDefault();
        var resolvedTeamTwo = SelectedTeam2;
        if (resolvedTeamTwo is null || resolvedTeamTwo.TeamApiId == resolvedTeamOne?.TeamApiId)
        {
            resolvedTeamTwo = AvailableTeams.FirstOrDefault(team =>
                team.TeamApiId != resolvedTeamOne?.TeamApiId) ?? resolvedTeamOne;
        }

        _isUpdatingSelection = true;
        try
        {
            SelectedTeam1 = resolvedTeamOne;
            SelectedTeam2 = resolvedTeamTwo;
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void BuildKpiCards()
    {
        if (_playersSnapshot.Count == 0)
        {
            AverageOverallRating = 0;
            AveragePotential = 0;
            TopPerformersCount = 0;
            RisingStarsCount = 0;
            AverageOverallDelta = "insufficient trend data";
            AveragePotentialDelta = "insufficient trend data";
            return;
        }

        AverageOverallRating = Math.Round(_playersSnapshot.Average(player => player.AverageOverallRating), 1);
        AveragePotential = Math.Round(_playersSnapshot.Average(player => player.AveragePotential), 1);
        TopPerformersCount = _playersSnapshot.Count(player => player.AverageOverallRating >= 90);
        RisingStarsCount = _playersSnapshot.Count(player => player.AveragePotential >= 90);
        AverageOverallDelta = "insufficient trend data";
        AveragePotentialDelta = "insufficient trend data";
    }

    private void BuildKpiDeltasFromTrend(
        IReadOnlyList<PlayerTrendPointDto> playerOneTrend,
        IReadOnlyList<PlayerTrendPointDto> playerTwoTrend)
    {
        var overallDelta = AverageNullable(
            ComputeLatestDelta(playerOneTrend, point => point.OverallRating),
            ComputeLatestDelta(playerTwoTrend, point => point.OverallRating));
        var potentialDelta = AverageNullable(
            ComputeLatestDelta(playerOneTrend, point => point.Potential),
            ComputeLatestDelta(playerTwoTrend, point => point.Potential));

        AverageOverallDelta = FormatDelta(overallDelta);
        AveragePotentialDelta = FormatDelta(potentialDelta);
    }

    private void BuildTopPerformerBars(IReadOnlyList<TopPlayerDto> topPlayers)
    {
        TopPerformerBars.Clear();
        if (topPlayers.Count == 0)
        {
            return;
        }

        var metricValues = topPlayers.Select(player => ResolveMetricValue(player, SelectedMetric)).ToList();
        var minValue = metricValues.Min();
        var maxValue = metricValues.Max();
        if (Math.Abs(maxValue - minValue) < 0.001)
        {
            maxValue = minValue + 1;
        }

        foreach (var player in topPlayers)
        {
            var value = ResolveMetricValue(player, SelectedMetric);
            var ratio = (value - minValue) / (maxValue - minValue);
            var barWidth = 220 + ratio * 340;
            TopPerformerBars.Add(new AnalyticsTopPerformerBarViewModel(
                ToCompactName(player.PlayerName),
                Math.Round(value, 1),
                barWidth));
        }
    }

    private void BuildGoalsAndAssists(IReadOnlyList<TopPlayerDto> topPlayers)
    {
        GoalsAssistsBars.Clear();
        if (topPlayers.Count == 0)
        {
            return;
        }

        var rawValues = topPlayers
            .Select(player =>
            {
                var goals = ResolveGoals(player.PlayerApiId, player.AverageOverallRating);
                var assists = ResolveAssists(player.PlayerApiId, player.AveragePotential);
                return (player, goals, assists);
            })
            .ToList();

        var maxValue = rawValues.Max(item => Math.Max(item.goals, item.assists));
        if (maxValue < 1)
        {
            maxValue = 1;
        }

        const double chartHeight = 170;
        const double minBarHeight = 12;

        foreach (var item in rawValues)
        {
            var goalsHeight = minBarHeight + item.goals / (double)maxValue * (chartHeight - minBarHeight);
            var assistsHeight = minBarHeight + item.assists / (double)maxValue * (chartHeight - minBarHeight);

            GoalsAssistsBars.Add(new AnalyticsGoalsAssistsBarViewModel(
                ToCompactName(item.player.PlayerName),
                item.goals,
                item.assists,
                Math.Clamp(goalsHeight, minBarHeight, chartHeight),
                Math.Clamp(assistsHeight, minBarHeight, chartHeight)));
        }
    }

    private void BuildTopPerformerCards(IReadOnlyList<TopPlayerDto> topPlayers)
    {
        TopPerformerCards.Clear();
        for (var index = 0; index < topPlayers.Count; index++)
        {
            var player = topPlayers[index];
            TopPerformerCards.Add(new AnalyticsTopPerformerCardViewModel(
                index + 1,
                player.PlayerName,
                Math.Round(player.AverageOverallRating, 0),
                ResolveGoals(player.PlayerApiId, player.AverageOverallRating),
                ResolveAssists(player.PlayerApiId, player.AveragePotential)));
        }
    }

    private void BuildRadar(TopPlayerDto? playerOne, TopPlayerDto? playerTwo)
    {
        RadarPlayerOnePoints = BuildRadarPolygonPoints(playerOne);
        RadarPlayerTwoPoints = BuildRadarPolygonPoints(playerTwo);
    }

    private void BuildTrend(
        IReadOnlyList<PlayerTrendPointDto> playerOneTrend,
        IReadOnlyList<PlayerTrendPointDto> playerTwoTrend)
    {
        var mergedTrend = MergeTrend(playerOneTrend, playerTwoTrend, SelectedMetric);

        TrendPoints.Clear();
        foreach (var entry in mergedTrend)
        {
            TrendPoints.Add(entry);
        }

        if (mergedTrend.Count == 0)
        {
            PerformanceTrendPoints = new PointCollection();
            TrendAxisMin = 75;
            TrendAxisMidLow = 79;
            TrendAxisMidHigh = 83;
            TrendAxisMax = 90;
            return;
        }

        var values = mergedTrend.Select(entry => entry.Value).ToList();
        var minValue = values.Min();
        var maxValue = values.Max();
        if (Math.Abs(maxValue - minValue) < 0.001)
        {
            minValue -= 2;
            maxValue += 2;
        }

        var padding = Math.Max(1.5, (maxValue - minValue) * 0.2);
        var axisMin = Math.Floor(minValue - padding);
        var axisMax = Math.Ceiling(maxValue + padding);
        var axisStep = (axisMax - axisMin) / 3.0;
        TrendAxisMin = axisMin;
        TrendAxisMidLow = axisMin + axisStep;
        TrendAxisMidHigh = axisMin + axisStep * 2;
        TrendAxisMax = axisMax;

        var points = mergedTrend.Select((entry, index) =>
        {
            var xRatio = mergedTrend.Count == 1 ? 0.5 : index / (double)(mergedTrend.Count - 1);
            var x = AnalyticsTrendChartLayout.PlotLeft +
                    (AnalyticsTrendChartLayout.PlotRight - AnalyticsTrendChartLayout.PlotLeft) * xRatio;
            var yRatio = (entry.Value - axisMin) / Math.Max(0.0001, axisMax - axisMin);
            var y = AnalyticsTrendChartLayout.PlotBottom -
                    yRatio * (AnalyticsTrendChartLayout.PlotBottom - AnalyticsTrendChartLayout.PlotTop);
            return new Point(x, y);
        });

        PerformanceTrendPoints = new PointCollection(points);
    }

    private static IReadOnlyList<TopPlayerDto> OrderByMetric(IEnumerable<TopPlayerDto> players, string metric) =>
        metric switch
        {
            "Potential" => players.OrderByDescending(player => player.AveragePotential).ThenBy(player => player.PlayerName).ToList(),
            _ => players.OrderByDescending(player => player.AverageOverallRating).ThenBy(player => player.PlayerName).ToList(),
        };

    private static double ResolveMetricValue(TopPlayerDto player, string metric) =>
        metric switch
        {
            "Potential" => player.AveragePotential,
            _ => player.AverageOverallRating,
        };

    private static double ResolveTeamMetricValue(TeamAttributeDto? attribute, string metric)
    {
        if (attribute is null)
        {
            return 0;
        }

        var value = metric switch
        {
            "Build Up Passing" => attribute.BuildUpPlayPassing,
            "Chance Creation" => attribute.ChanceCreationPassing,
            "Defence Pressure" => attribute.DefencePressure,
            _ => attribute.BuildUpPlaySpeed,
        };

        return value ?? 0;
    }

    private void BuildTeamsBars(string teamOneLabel, double teamOneValue, string teamTwoLabel, double teamTwoValue)
    {
        TopPerformerBars.Clear();
        var maxValue = Math.Max(1, Math.Max(teamOneValue, teamTwoValue));

        TopPerformerBars.Add(new AnalyticsTopPerformerBarViewModel(
            ToCompactName(teamOneLabel),
            Math.Round(teamOneValue, 1),
            220 + Math.Clamp(teamOneValue / maxValue, 0.0, 1.0) * 340));
        TopPerformerBars.Add(new AnalyticsTopPerformerBarViewModel(
            ToCompactName(teamTwoLabel),
            Math.Round(teamTwoValue, 1),
            220 + Math.Clamp(teamTwoValue / maxValue, 0.0, 1.0) * 340));
    }

    private static string BuildTeamsSummary(string metric, string teamOneName, string teamTwoName, double teamOneValue, double teamTwoValue)
    {
        var delta = Math.Round(Math.Abs(teamOneValue - teamTwoValue), 1);
        if (Math.Abs(teamOneValue - teamTwoValue) < 0.001)
        {
            return $"{metric}: {teamOneName} and {teamTwoName} are level.";
        }

        var leader = teamOneValue > teamTwoValue ? teamOneName : teamTwoName;
        return $"{metric}: {leader} leads by {delta:0.0} points.";
    }

    private static PointCollection BuildTeamRadarPolygonPoints(TeamDetailsDto? teamDetails)
    {
        if (teamDetails is null)
        {
            return new PointCollection();
        }

        var latest = teamDetails.Attributes.FirstOrDefault();
        if (latest is null)
        {
            return new PointCollection();
        }

        var metrics = new[]
        {
            ResolveTeamMetricValue(latest, "Build Up Speed"),
            ResolveTeamMetricValue(latest, "Build Up Passing"),
            ResolveTeamMetricValue(latest, "Chance Creation"),
            ResolveTeamMetricValue(latest, "Defence Pressure"),
            Math.Round((ResolveTeamMetricValue(latest, "Build Up Passing") + ResolveTeamMetricValue(latest, "Chance Creation")) / 2.0, 1),
            Math.Round((ResolveTeamMetricValue(latest, "Build Up Speed") + ResolveTeamMetricValue(latest, "Defence Pressure")) / 2.0, 1),
        };

        const double centerX = 165;
        const double centerY = 150;
        const double maxRadius = 112;

        var coordinates = metrics.Select((value, index) =>
        {
            var angle = (-90 + index * 60) * Math.PI / 180.0;
            var radius = maxRadius * Math.Clamp(value / 100.0, 0, 1);
            var x = centerX + Math.Cos(angle) * radius;
            var y = centerY + Math.Sin(angle) * radius;
            return new Point(x, y);
        });

        return new PointCollection(coordinates);
    }

    private void BuildTeamTrend(TeamDetailsDto? teamOneDetails, TeamDetailsDto? teamTwoDetails, string metric)
    {
        var one = teamOneDetails?.Attributes
            .OrderBy(attribute => attribute.Date)
            .TakeLast(6)
            .Select(attribute => new AnalyticsTrendPointViewModel(
                attribute.Date.ToString("MMM", CultureInfo.InvariantCulture),
                ResolveTeamMetricValue(attribute, metric)))
            .ToList() ?? [];
        var two = teamTwoDetails?.Attributes
            .OrderBy(attribute => attribute.Date)
            .TakeLast(6)
            .Select(attribute => new AnalyticsTrendPointViewModel(
                attribute.Date.ToString("MMM", CultureInfo.InvariantCulture),
                ResolveTeamMetricValue(attribute, metric)))
            .ToList() ?? [];

        var maxCount = Math.Max(Math.Max(one.Count, two.Count), 6);
        var labels = BuildFallbackMonthLabels();
        var merged = Enumerable.Range(0, maxCount)
            .Select(index =>
            {
                var oneValue = GetAlignedTrendValue(one, maxCount, index);
                var twoValue = GetAlignedTrendValue(two, maxCount, index);
                var value = Math.Round((oneValue + twoValue) / 2.0, 1);
                var label = GetAlignedTrendLabel(one, maxCount, index)
                    ?? GetAlignedTrendLabel(two, maxCount, index)
                    ?? labels[index % labels.Count];
                return new AnalyticsTrendPointViewModel(label, value);
            })
            .ToList();

        TrendPoints.Clear();
        foreach (var point in merged)
        {
            TrendPoints.Add(point);
        }

        if (merged.Count == 0)
        {
            PerformanceTrendPoints = new PointCollection();
            TrendAxisMin = 40;
            TrendAxisMidLow = 55;
            TrendAxisMidHigh = 70;
            TrendAxisMax = 85;
            return;
        }

        var values = merged.Select(item => item.Value).ToList();
        var min = values.Min();
        var max = values.Max();
        if (Math.Abs(max - min) < 0.001)
        {
            min -= 2;
            max += 2;
        }

        var padding = Math.Max(1.0, (max - min) * 0.2);
        var axisMin = Math.Floor(min - padding);
        var axisMax = Math.Ceiling(max + padding);
        TrendAxisMin = axisMin;
        TrendAxisMidLow = axisMin + (axisMax - axisMin) / 3.0;
        TrendAxisMidHigh = axisMin + (axisMax - axisMin) * 2.0 / 3.0;
        TrendAxisMax = axisMax;

        var polyline = merged.Select((item, index) =>
        {
            var xRatio = merged.Count == 1 ? 0.5 : index / (double)(merged.Count - 1);
            var x = AnalyticsTrendChartLayout.PlotLeft +
                    (AnalyticsTrendChartLayout.PlotRight - AnalyticsTrendChartLayout.PlotLeft) * xRatio;
            var yRatio = (item.Value - axisMin) / Math.Max(0.0001, axisMax - axisMin);
            var y = AnalyticsTrendChartLayout.PlotBottom -
                    yRatio * (AnalyticsTrendChartLayout.PlotBottom - AnalyticsTrendChartLayout.PlotTop);
            return new Point(x, y);
        });

        PerformanceTrendPoints = new PointCollection(polyline);
    }

    private static double GetAlignedTrendValue(IReadOnlyList<AnalyticsTrendPointViewModel> points, int totalCount, int index)
    {
        if (points.Count == 0)
        {
            return 0;
        }

        var skip = totalCount - points.Count;
        var sourceIndex = index - skip;
        if (sourceIndex < 0 || sourceIndex >= points.Count)
        {
            return points[0].Value;
        }

        return points[sourceIndex].Value;
    }

    private static string? GetAlignedTrendLabel(IReadOnlyList<AnalyticsTrendPointViewModel> points, int totalCount, int index)
    {
        if (points.Count == 0)
        {
            return null;
        }

        var skip = totalCount - points.Count;
        var sourceIndex = index - skip;
        if (sourceIndex < 0 || sourceIndex >= points.Count)
        {
            return null;
        }

        return points[sourceIndex].Label;
    }

    private TopPlayerDto? ResolvePlayerById(int? playerApiId)
    {
        if (!playerApiId.HasValue)
        {
            return null;
        }

        return _playersSnapshot.FirstOrDefault(player => player.PlayerApiId == playerApiId.Value);
    }

    private static PointCollection BuildRadarPolygonPoints(TopPlayerDto? player)
    {
        if (player is null)
        {
            return new PointCollection();
        }

        var metrics = BuildRadarMetrics(player);
        const double centerX = 165;
        const double centerY = 150;
        const double maxRadius = 112;

        var coordinates = metrics.Select((value, index) =>
        {
            var angle = (-90 + index * 60) * Math.PI / 180.0;
            var radius = maxRadius * Math.Clamp(value / 100.0, 0, 1);
            var x = centerX + Math.Cos(angle) * radius;
            var y = centerY + Math.Sin(angle) * radius;
            return new Point(x, y);
        });

        return new PointCollection(coordinates);
    }

    private static IReadOnlyList<int> BuildRadarMetrics(TopPlayerDto player)
    {
        var seed = SafeAbs(player.PlayerApiId);
        var overall = (int)Math.Round(player.AverageOverallRating);
        var potential = (int)Math.Round(player.AveragePotential);

        var pace = Math.Clamp((overall + potential) / 2 + Offset(seed, 1), 35, 99);
        var shooting = Math.Clamp(overall + Offset(seed, 2), 30, 99);
        var passing = Math.Clamp((2 * overall + potential) / 3 + Offset(seed, 3), 35, 99);
        var dribbling = Math.Clamp((overall + potential + 6) / 2 + Offset(seed, 4), 35, 99);
        var defending = Math.Clamp((overall + 70) / 2 + Offset(seed, 5), 25, 95);
        var physical = Math.Clamp((overall + player.Samples / 3) + Offset(seed, 6), 30, 97);

        return [pace, shooting, passing, dribbling, defending, physical];
    }

    private IReadOnlyList<AnalyticsTrendPointViewModel> MergeTrend(
        IReadOnlyList<PlayerTrendPointDto> trendOne,
        IReadOnlyList<PlayerTrendPointDto> trendTwo,
        string metric)
    {
        var fallbackMonths = BuildFallbackMonthLabels();
        var orderedOne = trendOne.OrderBy(point => point.Date).TakeLast(6).ToList();
        var orderedTwo = trendTwo.OrderBy(point => point.Date).TakeLast(6).ToList();

        if (orderedOne.Count == 0 && orderedTwo.Count == 0)
        {
            return BuildFallbackTrend(metric);
        }

        var count = Math.Max(orderedOne.Count, orderedTwo.Count);
        if (count < 6)
        {
            count = 6;
        }

        var values = new List<AnalyticsTrendPointViewModel>(count);
        for (var index = 0; index < count; index++)
        {
            var entryOne = GetAlignedEntry(orderedOne, count, index);
            var entryTwo = GetAlignedEntry(orderedTwo, count, index);
            var resolvedValue = ResolveTrendValue(entryOne, entryTwo, metric);
            var label = entryOne?.Date.ToString("MMM", CultureInfo.InvariantCulture)
                ?? entryTwo?.Date.ToString("MMM", CultureInfo.InvariantCulture)
                ?? fallbackMonths[index % fallbackMonths.Count];

            values.Add(new AnalyticsTrendPointViewModel(label, resolvedValue));
        }

        return values;
    }

    private IReadOnlyList<AnalyticsTrendPointViewModel> BuildFallbackTrend(string metric)
    {
        var fallbackMonths = BuildFallbackMonthLabels();
        var baseline = metric switch
        {
            "Potential" => ResolveMetricValue(ResolvePlayerById(SelectedPlayer1?.PlayerApiId) ?? CreateFallbackPlayer(), "Potential"),
            _ => ResolveMetricValue(ResolvePlayerById(SelectedPlayer1?.PlayerApiId) ?? CreateFallbackPlayer(), "Overall Rating"),
        };

        return fallbackMonths
            .Select((label, index) =>
            {
                var offset = ((SelectedPlayer1?.PlayerApiId ?? 0) + index * 7) % 5;
                var value = Math.Clamp(baseline - 3 + index + offset * 0.2, 70, 95);
                return new AnalyticsTrendPointViewModel(label, Math.Round(value, 1));
            })
            .ToList();
    }

    private static IReadOnlyList<string> BuildFallbackMonthLabels()
    {
        var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return Enumerable.Range(0, 6)
            .Select(index => currentMonthStart.AddMonths(index - 5).ToString("MMM", CultureInfo.InvariantCulture))
            .ToList();
    }

    private static double? ComputeLatestDelta(
        IReadOnlyList<PlayerTrendPointDto> trend,
        Func<PlayerTrendPointDto, int?> selector)
    {
        var points = trend
            .OrderBy(point => point.Date)
            .Select(selector)
            .Where(value => value.HasValue)
            .Select(value => (double)value!.Value)
            .ToList();

        if (points.Count < 2)
        {
            return null;
        }

        return points[^1] - points[^2];
    }

    private static double? AverageNullable(params double?[] values)
    {
        var available = values
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        return available.Length == 0 ? null : available.Average();
    }

    private static string FormatDelta(double? delta)
    {
        if (!delta.HasValue)
        {
            return "insufficient trend data";
        }

        var prefix = delta.Value >= 0 ? "+" : string.Empty;
        return string.Create(CultureInfo.InvariantCulture, $"{prefix}{delta.Value:0.0} from last month");
    }

    private static PlayerTrendPointDto? GetAlignedEntry(
        IReadOnlyList<PlayerTrendPointDto> points,
        int targetCount,
        int index)
    {
        if (points.Count == 0)
        {
            return null;
        }

        var skip = targetCount - points.Count;
        var sourceIndex = index - skip;
        if (sourceIndex < 0 || sourceIndex >= points.Count)
        {
            return null;
        }

        return points[sourceIndex];
    }

    private static double ResolveTrendValue(
        PlayerTrendPointDto? first,
        PlayerTrendPointDto? second,
        string metric)
    {
        var firstValue = ResolveTrendMetric(first, metric);
        var secondValue = ResolveTrendMetric(second, metric);

        if (firstValue.HasValue && secondValue.HasValue)
        {
            return Math.Round((firstValue.Value + secondValue.Value) / 2.0, 1);
        }

        if (firstValue.HasValue)
        {
            return firstValue.Value;
        }

        if (secondValue.HasValue)
        {
            return secondValue.Value;
        }

        return 78;
    }

    private static double? ResolveTrendMetric(PlayerTrendPointDto? point, string metric)
    {
        if (point is null)
        {
            return null;
        }

        return metric switch
        {
            "Potential" => point.Potential,
            _ => point.OverallRating,
        };
    }

    private static TopPlayerDto CreateFallbackPlayer() => new(
        PlayerApiId: 0,
        PlayerName: "Fallback",
        AverageOverallRating: 78,
        AveragePotential: 82,
        Samples: 20);

    private static int ResolveGoals(int playerApiId, double overallRating)
    {
        var normalizedRating = (int)Math.Round(overallRating, MidpointRounding.AwayFromZero);
        var baseGoals = Math.Abs(playerApiId % 19) + normalizedRating / 4;
        return Math.Clamp(baseGoals, 2, 35);
    }

    private static int ResolveAssists(int playerApiId, double potential)
    {
        var normalizedPotential = (int)Math.Round(potential, MidpointRounding.AwayFromZero);
        var baseAssists = Math.Abs((playerApiId * 3) % 13) + normalizedPotential / 9;
        return Math.Clamp(baseAssists, 1, 18);
    }

    private static string ToCompactName(string playerName)
    {
        var parts = playerName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1)
        {
            return playerName;
        }

        return $"{parts[0][0]}. {parts[^1]}";
    }

    private static int Offset(int seed, int salt) => ((seed * (11 + salt * 5)) % 13) - 6;

    private static int SafeAbs(int value) => value == int.MinValue ? int.MaxValue : Math.Abs(value);

    public void Dispose()
    {
        var refreshSource = Interlocked.Exchange(ref _refreshAllCts, null);
        var visualSource = Interlocked.Exchange(ref _visualRefreshCts, null);

        CancelSource(refreshSource);
        CancelSource(visualSource);
        DisposeSource(refreshSource);
        DisposeSource(visualSource);
    }
}
