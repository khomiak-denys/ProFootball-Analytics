using System.Collections.ObjectModel;
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

    public DashboardViewModel(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
        SeasonOptions = new ObservableCollection<string>();
        LeagueOptions = new ObservableCollection<LeagueDto>();
        RecentMatches = new ObservableCollection<MatchListItemDto>();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, CommandExceptionHandler.Handle);
    }

    public ObservableCollection<string> SeasonOptions { get; }

    public ObservableCollection<LeagueDto> LeagueOptions { get; }

    public ObservableCollection<MatchListItemDto> RecentMatches { get; }

    public string CurrentDateLabel => _lastUpdatedAt.ToString("dd MMM yyyy HH:mm");

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
}
