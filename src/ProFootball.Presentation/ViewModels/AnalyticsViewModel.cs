using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Analytics;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class AnalyticsViewModel : ObservableObject
{
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private int _trendPlayerApiId;
    private int? _minOverall;
    private int? _minPotential;
    private string? _preferredFoot;
    private int _topLimit = 20;
    private int? _matchesLeagueId;

    public AnalyticsViewModel(IAnalyticsQueryService analyticsQueryService)
    {
        _analyticsQueryService = analyticsQueryService;
        TrendPoints = new ObservableCollection<PlayerTrendPointDto>();
        TopPlayers = new ObservableCollection<TopPlayerDto>();
        MatchesBySeason = new ObservableCollection<MatchesBySeasonDto>();

        LoadTrendCommand = new AsyncRelayCommand(LoadTrendAsync);
        LoadTopPlayersCommand = new AsyncRelayCommand(LoadTopPlayersAsync);
        LoadMatchesBySeasonCommand = new AsyncRelayCommand(LoadMatchesBySeasonAsync);
        RefreshAllCommand = new AsyncRelayCommand(RefreshAllAsync);
    }

    public ObservableCollection<PlayerTrendPointDto> TrendPoints { get; }

    public ObservableCollection<TopPlayerDto> TopPlayers { get; }

    public ObservableCollection<MatchesBySeasonDto> MatchesBySeason { get; }

    public int TrendPlayerApiId
    {
        get => _trendPlayerApiId;
        set => SetProperty(ref _trendPlayerApiId, value);
    }

    public int? MinOverall
    {
        get => _minOverall;
        set => SetProperty(ref _minOverall, value);
    }

    public int? MinPotential
    {
        get => _minPotential;
        set => SetProperty(ref _minPotential, value);
    }

    public string? PreferredFoot
    {
        get => _preferredFoot;
        set => SetProperty(ref _preferredFoot, value);
    }

    public int TopLimit
    {
        get => _topLimit;
        set => SetProperty(ref _topLimit, value);
    }

    public int? MatchesLeagueId
    {
        get => _matchesLeagueId;
        set => SetProperty(ref _matchesLeagueId, value);
    }

    public AsyncRelayCommand LoadTrendCommand { get; }

    public AsyncRelayCommand LoadTopPlayersCommand { get; }

    public AsyncRelayCommand LoadMatchesBySeasonCommand { get; }

    public AsyncRelayCommand RefreshAllCommand { get; }

    public async Task RefreshAllAsync()
    {
        await LoadTopPlayersAsync();
        await LoadMatchesBySeasonAsync();
        if (TrendPlayerApiId > 0)
        {
            await LoadTrendAsync();
        }
    }

    public async Task LoadTrendAsync()
    {
        TrendPoints.Clear();
        if (TrendPlayerApiId <= 0)
        {
            return;
        }

        var trend = await _analyticsQueryService.GetPlayerTrendAsync(TrendPlayerApiId);
        foreach (var point in trend)
        {
            TrendPoints.Add(point);
        }
    }

    public async Task LoadTopPlayersAsync()
    {
        TopPlayers.Clear();
        var players = await _analyticsQueryService.GetTopPlayersAsync(new TopPlayersQuery(
            TopLimit,
            MinOverall,
            MinPotential,
            PreferredFoot,
            "overall",
            true));

        foreach (var player in players)
        {
            TopPlayers.Add(player);
        }
    }

    public async Task LoadMatchesBySeasonAsync()
    {
        MatchesBySeason.Clear();
        var values = await _analyticsQueryService.GetMatchesBySeasonAsync(MatchesLeagueId);
        foreach (var item in values)
        {
            MatchesBySeason.Add(item);
        }
    }
}
