using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly IQueryDispatcher _queryDispatcher;

    private int _countries;
    private int _leagues;
    private int _teams;
    private int _players;
    private int _matches;

    public DashboardViewModel(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, CommandExceptionHandler.Handle);
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
        var kpis = await _queryDispatcher.DispatchAsync<GetDashboardKpiQuery, DashboardKpiDto>(new GetDashboardKpiQuery());
        Countries = kpis.Countries;
        Leagues = kpis.Leagues;
        Teams = kpis.Teams;
        Players = kpis.Players;
        Matches = kpis.Matches;
    }
}
