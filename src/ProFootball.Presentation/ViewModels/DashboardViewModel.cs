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

        InitializeSeasonOptions();
    }

    public ObservableCollection<string> SeasonOptions { get; }

    public ObservableCollection<LeagueDto> LeagueOptions { get; }

    public ObservableCollection<MatchListItemDto> RecentMatches { get; }

    public string CurrentDateLabel => _lastUpdatedAt.ToString("dd MMM yyyy HH:mm");

    public int CurrentYear => DateTime.Now.Year;

    public string? SelectedSeason
    {
        get => _selectedSeason;
        set => SetProperty(ref _selectedSeason, value);
    }

    public LeagueDto? SelectedLeague
    {
        get => _selectedLeague;
        set => SetProperty(ref _selectedLeague, value);
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

    private void InitializeSeasonOptions()
    {
        var today = DateTime.Today;
        var currentSeasonStartYear = today.Month >= 7 ? today.Year : today.Year - 1;

        for (var offset = 0; offset < 4; offset++)
        {
            var startYear = currentSeasonStartYear - offset;
            var endYear = startYear + 1;
            SeasonOptions.Add($"{startYear}/{endYear}");
        }

        SelectedSeason = SeasonOptions[0];
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
