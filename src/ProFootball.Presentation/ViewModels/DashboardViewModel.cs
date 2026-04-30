using System.Collections.ObjectModel;
using System.Globalization;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.League.Dtos;
using ProFootball.Application.League.Queries;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly IQueryDispatcher _queryDispatcher;
    private string? _selectedSeason;
    private LeagueDto? _selectedLeague;
    private bool _isUpdatingFilters;
    private DateTime _lastUpdatedAt = DateTime.Now;

    private int _countries;
    private int _leagues;
    private int _teams;
    private int _players;
    private int _matches;
    private string _averageGoalsPerMatchLabel = "0.00";
    private int _homeWins;
    private int _draws;
    private int _awayWins;

    public DashboardViewModel(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
        SeasonOptions = new ObservableCollection<string>();
        LeagueOptions = new ObservableCollection<LeagueDto>();
        RecentMatches = new ObservableCollection<MatchListItemDto>();
        SeasonTrend = new ObservableCollection<DashboardTrendPointViewModel>();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, CommandExceptionHandler.Handle);
    }

    public ObservableCollection<string> SeasonOptions { get; }

    public ObservableCollection<LeagueDto> LeagueOptions { get; }

    public ObservableCollection<MatchListItemDto> RecentMatches { get; }

    public ObservableCollection<DashboardTrendPointViewModel> SeasonTrend { get; }

    public string CurrentDateLabel => _lastUpdatedAt.ToString("dd MMM yyyy HH:mm");

    public string AverageGoalsPerMatchLabel
    {
        get => _averageGoalsPerMatchLabel;
        private set => SetProperty(ref _averageGoalsPerMatchLabel, value);
    }

    public int HomeWins
    {
        get => _homeWins;
        private set => SetProperty(ref _homeWins, value);
    }

    public int Draws
    {
        get => _draws;
        private set => SetProperty(ref _draws, value);
    }

    public int AwayWins
    {
        get => _awayWins;
        private set => SetProperty(ref _awayWins, value);
    }

    public int CurrentYear => DateTime.Now.Year;

    public string? SelectedSeason
    {
        get => _selectedSeason;
        set
        {
            if (!SetProperty(ref _selectedSeason, value) || _isUpdatingFilters)
            {
                return;
            }

            _ = ReloadRecentMatchesSafeAsync();
        }
    }

    public LeagueDto? SelectedLeague
    {
        get => _selectedLeague;
        set
        {
            if (!SetProperty(ref _selectedLeague, value) || _isUpdatingFilters)
            {
                return;
            }

            _ = ReloadByLeagueSafeAsync();
        }
    }

    public int Countries
    {
        get => _countries;
        private set => SetProperty(ref _countries, value);
    }

    public int Leagues
    {
        get => _leagues;
        private set => SetProperty(ref _leagues, value);
    }

    public int Teams
    {
        get => _teams;
        private set => SetProperty(ref _teams, value);
    }

    public int Players
    {
        get => _players;
        private set => SetProperty(ref _players, value);
    }

    public int Matches
    {
        get => _matches;
        private set => SetProperty(ref _matches, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }

    public async Task RefreshAsync()
    {
        if (LeagueOptions.Count == 0)
        {
            await LoadLeaguesAsync();
        }

        await LoadSeasonOptionsAsync();
        await LoadRecentMatchesAsync();

        var kpis = await _queryDispatcher.DispatchAsync<GetDashboardKpiQuery, DashboardKpiDto>(new GetDashboardKpiQuery());
        Countries = kpis.Countries;
        Leagues = kpis.Leagues;
        Teams = kpis.Teams;
        Players = kpis.Players;
        Matches = kpis.Matches;

        _lastUpdatedAt = DateTime.Now;

        RaisePropertyChanged(nameof(CurrentDateLabel));
        RaisePropertyChanged(nameof(CurrentYear));
        await LoadChartsAsync();
    }

    private async Task LoadSeasonOptionsAsync()
    {
        int? selectedLeagueId = SelectedLeague is { Id: > 0 } league ? league.Id : null;
        var groupedBySeason = await _queryDispatcher.DispatchAsync<GetMatchesBySeasonQuery, IReadOnlyList<MatchesBySeasonDto>>(
            new GetMatchesBySeasonQuery(selectedLeagueId));
        var seasons = groupedBySeason
            .Select(item => item.Season)
            .Where(season => !string.IsNullOrWhiteSpace(season))
            .Select(season => season.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(season => season, StringComparer.Ordinal)
            .ToList();

        _isUpdatingFilters = true;
        try
        {
            var previousSelection = SelectedSeason;
            SeasonOptions.Clear();
            foreach (var season in seasons)
            {
                SeasonOptions.Add(season);
            }

            if (!string.IsNullOrWhiteSpace(previousSelection) && SeasonOptions.Contains(previousSelection))
            {
                SelectedSeason = previousSelection;
            }
            else
            {
                SelectedSeason = SeasonOptions.FirstOrDefault();
            }
        }
        finally
        {
            _isUpdatingFilters = false;
        }
    }

    private async Task LoadLeaguesAsync()
    {
        LeagueOptions.Clear();
        LeagueOptions.Add(new LeagueDto(0, "All Leagues", string.Empty));

        var leagues = await _queryDispatcher.DispatchAsync<GetLeaguesQuery, IReadOnlyList<LeagueDto>>(new GetLeaguesQuery());
        foreach (var league in leagues.OrderBy(l => l.Name))
        {
            LeagueOptions.Add(league);
        }

        SelectedLeague ??= LeagueOptions[0];
    }

    private async Task ReloadByLeagueSafeAsync()
    {
        try
        {
            await LoadSeasonOptionsAsync();
            await LoadRecentMatchesAsync();
            await LoadChartsAsync();
        }
        catch (Exception exception)
        {
            CommandExceptionHandler.Handle(exception);
        }
    }

    private async Task ReloadRecentMatchesSafeAsync()
    {
        try
        {
            await LoadRecentMatchesAsync();
            await LoadChartsAsync();
        }
        catch (Exception exception)
        {
            CommandExceptionHandler.Handle(exception);
        }
    }

    private async Task LoadRecentMatchesAsync()
    {
        int? selectedLeagueId = SelectedLeague is { Id: > 0 } league ? league.Id : null;
        var season = string.IsNullOrWhiteSpace(SelectedSeason) ? null : SelectedSeason;

        var result = await _queryDispatcher.DispatchAsync<MatchSearchQuery, ProFootball.Application.Common.PagedResult<MatchListItemDto>>(
            new MatchSearchQuery(
                selectedLeagueId,
                season,
                null,
                null,
                null,
                "date",
                true,
                1,
                3));

        RecentMatches.Clear();
        foreach (var match in result.Items)
        {
            RecentMatches.Add(match);
        }
    }

    private async Task LoadChartsAsync()
    {
        int? selectedLeagueId = SelectedLeague is { Id: > 0 } league ? league.Id : null;
        var season = string.IsNullOrWhiteSpace(SelectedSeason) ? null : SelectedSeason;

        var trendTask = _queryDispatcher.DispatchAsync<GetSeasonRatingTrendQuery, IReadOnlyList<SeasonRatingTrendPointDto>>(
            new GetSeasonRatingTrendQuery(season, selectedLeagueId));
        var outcomeTask = _queryDispatcher.DispatchAsync<GetMatchOutcomeDistributionQuery, MatchOutcomeDistributionDto>(
            new GetMatchOutcomeDistributionQuery(season, selectedLeagueId));

        await Task.WhenAll(trendTask, outcomeTask);
        var trend = await trendTask;
        var outcome = await outcomeTask;

        SeasonTrend.Clear();
        foreach (var point in trend.TakeLast(6))
        {
            SeasonTrend.Add(new DashboardTrendPointViewModel(
                point.Month.ToString("MMM", CultureInfo.InvariantCulture),
                Math.Round(point.AverageOverallRating, 2)));
        }

        HomeWins = outcome.HomeWins;
        Draws = outcome.Draws;
        AwayWins = outcome.AwayWins;
        AverageGoalsPerMatchLabel = outcome.AverageGoalsPerMatch.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
