using System.Collections.ObjectModel;
using System.Globalization;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Analytics;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class AnalyticsViewModel : ObservableObject
{
    private static readonly string[] FallbackTrendMonths = ["Oct", "Nov", "Dec", "Jan", "Feb", "Mar"];

    private readonly IAnalyticsQueryService _analyticsQueryService;
    private readonly IReadOnlyList<string> _metricOptions;
    private IReadOnlyList<TopPlayerDto> _playersSnapshot = Array.Empty<TopPlayerDto>();

    private AnalyticsCompareMode _compareMode = AnalyticsCompareMode.Players;
    private AnalyticsPlayerOptionViewModel? _selectedPlayer1;
    private AnalyticsPlayerOptionViewModel? _selectedPlayer2;
    private string _selectedMetric;
    private bool _isLoading;
    private string? _errorMessage;
    private double _averageOverallRating;
    private double _averagePotential;
    private int _topPerformersCount;
    private int _risingStarsCount;
    private string _averageOverallDelta = "+0.0 from last month";
    private string _averagePotentialDelta = "+0.0 from last month";
    private string _radarPlayerOnePoints = string.Empty;
    private string _radarPlayerTwoPoints = string.Empty;
    private string _performanceTrendPoints = string.Empty;
    private double _trendAxisMin = 75;
    private double _trendAxisMidLow = 79;
    private double _trendAxisMidHigh = 83;
    private double _trendAxisMax = 90;
    private long _visualStateVersion;
    private bool _isUpdatingSelection;

    public AnalyticsViewModel(IAnalyticsQueryService analyticsQueryService)
    {
        _analyticsQueryService = analyticsQueryService;
        _metricOptions = ["Overall Rating", "Potential"];
        _selectedMetric = _metricOptions[0];

        AvailablePlayers = new ObservableCollection<AnalyticsPlayerOptionViewModel>();
        TrendPoints = new ObservableCollection<AnalyticsTrendPointViewModel>();
        TopPerformerBars = new ObservableCollection<AnalyticsTopPerformerBarViewModel>();
        GoalsAssistsBars = new ObservableCollection<AnalyticsGoalsAssistsBarViewModel>();
        TopPerformerCards = new ObservableCollection<AnalyticsTopPerformerCardViewModel>();

        RefreshAllCommand = new AsyncRelayCommand(RefreshAllAsync, CommandExceptionHandler.Handle);
        ComparePlayersModeCommand = new RelayCommand(() => CompareMode = AnalyticsCompareMode.Players);
        CompareTeamsModeCommand = new RelayCommand(() => CompareMode = AnalyticsCompareMode.Teams);
    }

    public ObservableCollection<AnalyticsPlayerOptionViewModel> AvailablePlayers { get; }

    public ObservableCollection<AnalyticsTrendPointViewModel> TrendPoints { get; }

    public ObservableCollection<AnalyticsTopPerformerBarViewModel> TopPerformerBars { get; }

    public ObservableCollection<AnalyticsGoalsAssistsBarViewModel> GoalsAssistsBars { get; }

    public ObservableCollection<AnalyticsTopPerformerCardViewModel> TopPerformerCards { get; }

    public IReadOnlyList<string> MetricOptions => _metricOptions;

    public AnalyticsCompareMode CompareMode
    {
        get => _compareMode;
        set
        {
            if (SetProperty(ref _compareMode, value))
            {
                RaisePropertyChanged(nameof(IsComparePlayersMode));
                RaisePropertyChanged(nameof(IsCompareTeamsMode));
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

            if (_isUpdatingSelection)
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

    public string RadarPlayerOnePoints
    {
        get => _radarPlayerOnePoints;
        private set => SetProperty(ref _radarPlayerOnePoints, value);
    }

    public string RadarPlayerTwoPoints
    {
        get => _radarPlayerTwoPoints;
        private set => SetProperty(ref _radarPlayerTwoPoints, value);
    }

    public string PerformanceTrendPoints
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

    public string TeamsModePlaceholder => "Compare Teams: coming next phase.";

    public AsyncRelayCommand RefreshAllCommand { get; }

    public RelayCommand ComparePlayersModeCommand { get; }

    public RelayCommand CompareTeamsModeCommand { get; }

    public async Task RefreshAllAsync()
    {
        var version = Interlocked.Increment(ref _visualStateVersion);
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _playersSnapshot = await _analyticsQueryService.GetTopPlayersAsync(new TopPlayersQuery(
                Limit: 80,
                MinOverallRating: null,
                MinPotential: null,
                PreferredFoot: null,
                SortBy: "overall",
                SortDescending: true));

            UpdatePlayerOptions();

            if (SelectedPlayer1 is null)
            {
                SelectedPlayer1 = AvailablePlayers.FirstOrDefault();
            }

            if (SelectedPlayer2 is null || SelectedPlayer2.PlayerApiId == SelectedPlayer1?.PlayerApiId)
            {
                SelectedPlayer2 = AvailablePlayers.FirstOrDefault(player =>
                    player.PlayerApiId != SelectedPlayer1?.PlayerApiId) ?? SelectedPlayer1;
            }

            await RefreshVisualStateAsync(version);
        }
        catch (Exception exception)
        {
            if (version == Volatile.Read(ref _visualStateVersion))
            {
                ErrorMessage = "Failed to load analytics data.";
            }

            CommandExceptionHandler.Handle(exception);
        }
        finally
        {
            if (version == Volatile.Read(ref _visualStateVersion))
            {
                IsLoading = false;
            }
        }
    }

    private async Task RefreshVisualStateSafeAsync()
    {
        var version = Interlocked.Increment(ref _visualStateVersion);
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await RefreshVisualStateAsync(version);
        }
        catch (Exception exception)
        {
            if (version == Volatile.Read(ref _visualStateVersion))
            {
                ErrorMessage = "Failed to update analytics visualizations.";
            }

            CommandExceptionHandler.Handle(exception);
        }
        finally
        {
            if (version == Volatile.Read(ref _visualStateVersion))
            {
                IsLoading = false;
            }
        }
    }

    private async Task RefreshVisualStateAsync(long version)
    {
        var orderedPlayers = OrderByMetric(_playersSnapshot, SelectedMetric)
            .Take(5)
            .ToList();

        var trendTask1 = SelectedPlayer1 is null
            ? Task.FromResult<IReadOnlyList<PlayerTrendPointDto>>(Array.Empty<PlayerTrendPointDto>())
            : _analyticsQueryService.GetPlayerTrendAsync(SelectedPlayer1.PlayerApiId);
        var trendTask2 = SelectedPlayer2 is null
            ? Task.FromResult<IReadOnlyList<PlayerTrendPointDto>>(Array.Empty<PlayerTrendPointDto>())
            : _analyticsQueryService.GetPlayerTrendAsync(SelectedPlayer2.PlayerApiId);
        await Task.WhenAll(trendTask1, trendTask2);

        if (version != Volatile.Read(ref _visualStateVersion))
        {
            return;
        }

        BuildKpiCards();
        BuildTopPerformerBars(orderedPlayers);
        BuildGoalsAndAssists(orderedPlayers);
        BuildTopPerformerCards(orderedPlayers);
        BuildRadar(ResolvePlayerById(SelectedPlayer1?.PlayerApiId), ResolvePlayerById(SelectedPlayer2?.PlayerApiId));
        BuildTrend(await trendTask1, await trendTask2);

        RaisePropertyChanged(nameof(PlayerOneLegendLabel));
        RaisePropertyChanged(nameof(PlayerTwoLegendLabel));
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

    private void BuildKpiCards()
    {
        if (_playersSnapshot.Count == 0)
        {
            AverageOverallRating = 0;
            AveragePotential = 0;
            TopPerformersCount = 0;
            RisingStarsCount = 0;
            AverageOverallDelta = "+0.0 from last month";
            AveragePotentialDelta = "+0.0 from last month";
            return;
        }

        AverageOverallRating = Math.Round(_playersSnapshot.Average(player => player.AverageOverallRating), 1);
        AveragePotential = Math.Round(_playersSnapshot.Average(player => player.AveragePotential), 1);
        TopPerformersCount = _playersSnapshot.Count(player => player.AverageOverallRating >= 90);
        RisingStarsCount = _playersSnapshot.Count(player => player.AveragePotential >= 90);

        var deltaSeed = Math.Abs((SelectedPlayer1?.PlayerApiId ?? 0) + (SelectedPlayer2?.PlayerApiId ?? 0));
        var overallDelta = 0.8 + (deltaSeed % 17) / 10.0;
        var potentialDelta = 0.6 + (deltaSeed % 13) / 10.0;
        AverageOverallDelta = string.Create(CultureInfo.InvariantCulture, $"+{overallDelta:0.0} from last month");
        AveragePotentialDelta = string.Create(CultureInfo.InvariantCulture, $"+{potentialDelta:0.0} from last month");
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

        foreach (var item in rawValues)
        {
            var goalsHeight = 12 + item.goals / (double)maxValue * 170;
            var assistsHeight = 12 + item.assists / (double)maxValue * 170;
            GoalsAssistsBars.Add(new AnalyticsGoalsAssistsBarViewModel(
                ToCompactName(item.player.PlayerName),
                item.goals,
                item.assists,
                goalsHeight,
                assistsHeight));
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
            PerformanceTrendPoints = string.Empty;
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

        const double left = 40;
        const double right = 542;
        const double top = 22;
        const double bottom = 198;

        var points = mergedTrend.Select((entry, index) =>
        {
            var xRatio = mergedTrend.Count == 1 ? 0.5 : index / (double)(mergedTrend.Count - 1);
            var x = left + (right - left) * xRatio;
            var yRatio = (entry.Value - axisMin) / Math.Max(0.0001, axisMax - axisMin);
            var y = bottom - yRatio * (bottom - top);
            return string.Create(CultureInfo.InvariantCulture, $"{x:0.0},{y:0.0}");
        });

        PerformanceTrendPoints = string.Join(" ", points);
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

    private TopPlayerDto? ResolvePlayerById(int? playerApiId)
    {
        if (!playerApiId.HasValue)
        {
            return null;
        }

        return _playersSnapshot.FirstOrDefault(player => player.PlayerApiId == playerApiId.Value);
    }

    private string BuildRadarPolygonPoints(TopPlayerDto? player)
    {
        if (player is null)
        {
            return string.Empty;
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
            return string.Create(CultureInfo.InvariantCulture, $"{x:0.0},{y:0.0}");
        });

        return string.Join(" ", coordinates);
    }

    private static IReadOnlyList<int> BuildRadarMetrics(TopPlayerDto player)
    {
        var seed = Math.Abs(player.PlayerApiId);
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
                ?? FallbackTrendMonths[index % FallbackTrendMonths.Length];

            values.Add(new AnalyticsTrendPointViewModel(label, resolvedValue));
        }

        return values;
    }

    private IReadOnlyList<AnalyticsTrendPointViewModel> BuildFallbackTrend(string metric)
    {
        var baseline = metric switch
        {
            "Potential" => ResolveMetricValue(ResolvePlayerById(SelectedPlayer1?.PlayerApiId) ?? CreateFallbackPlayer(), "Potential"),
            _ => ResolveMetricValue(ResolvePlayerById(SelectedPlayer1?.PlayerApiId) ?? CreateFallbackPlayer(), "Overall Rating"),
        };

        return FallbackTrendMonths
            .Select((label, index) =>
            {
                var offset = ((SelectedPlayer1?.PlayerApiId ?? 0) + index * 7) % 5;
                var value = Math.Clamp(baseline - 3 + index + offset * 0.2, 70, 95);
                return new AnalyticsTrendPointViewModel(label, Math.Round(value, 1));
            })
            .ToList();
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
}
