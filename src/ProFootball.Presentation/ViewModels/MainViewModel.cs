using Microsoft.Extensions.Logging;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private AppTab _selectedTab = AppTab.Dashboard;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(
        IDashboardQueryService dashboardQueryService,
        ICountriesLeaguesQueryService countriesLeaguesQueryService,
        ITeamsQueryService teamsQueryService,
        IPlayersQueryService playersQueryService,
        IMatchesQueryService matchesQueryService,
        IAnalyticsQueryService analyticsQueryService,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MainViewModel>();
        Dashboard = new DashboardViewModel(dashboardQueryService);
        CountriesLeagues = new CountriesLeaguesViewModel(
            countriesLeaguesQueryService,
            loggerFactory.CreateLogger<CountriesLeaguesViewModel>());
        TeamDetails = new TeamDetailsViewModel(teamsQueryService);
        PlayerDetails = new PlayerDetailsViewModel(playersQueryService, analyticsQueryService);
        MatchDetails = new MatchDetailsViewModel(matchesQueryService);

        Teams = new TeamsViewModel(teamsQueryService, OpenTeamDetails);
        Players = new PlayersViewModel(playersQueryService, OpenPlayerDetails);
        Matches = new MatchesViewModel(matchesQueryService, countriesLeaguesQueryService, OpenMatchDetails);
        Analytics = new AnalyticsViewModel(analyticsQueryService);

        LoadInitialDataCommand = new AsyncRelayCommand(LoadInitialDataAsync, OnBackgroundCommandException);
    }

    public DashboardViewModel Dashboard { get; }

    public CountriesLeaguesViewModel CountriesLeagues { get; }

    public TeamsViewModel Teams { get; }

    public TeamDetailsViewModel TeamDetails { get; }

    public PlayersViewModel Players { get; }

    public PlayerDetailsViewModel PlayerDetails { get; }

    public MatchesViewModel Matches { get; }

    public MatchDetailsViewModel MatchDetails { get; }

    public AnalyticsViewModel Analytics { get; }

    public AppTab SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetProperty(ref _selectedTab, value))
            {
                RaisePropertyChanged(nameof(CurrentSectionTitle));
                RaisePropertyChanged(nameof(SelectedSidebarTab));
            }
        }
    }

    public AppTab SelectedSidebarTab
    {
        get => SelectedTab switch
        {
            AppTab.TeamDetails => AppTab.Teams,
            AppTab.PlayerDetails => AppTab.Players,
            AppTab.MatchDetails => AppTab.Matches,
            _ => SelectedTab,
        };
        set
        {
            if (SelectedTab != value)
            {
                SelectedTab = value;
            }
        }
    }

    public string CurrentSectionTitle => SelectedTab switch
    {
        AppTab.Dashboard => "Dashboard",
        AppTab.CountriesLeagues => "Leagues",
        AppTab.Teams => "Teams",
        AppTab.TeamDetails => "Team Details",
        AppTab.Players => "Players",
        AppTab.PlayerDetails => "Player Details",
        AppTab.Matches => "Matches",
        AppTab.MatchDetails => "Match Details",
        AppTab.Analytics => "Analytics",
        _ => "Dashboard",
    };

    public AsyncRelayCommand LoadInitialDataCommand { get; }

    public async Task LoadInitialDataAsync()
    {
        await Dashboard.RefreshAsync();
        await CountriesLeagues.RefreshAsync();
        await Teams.SearchAsync();
        await Players.SearchAsync();
        await Matches.LoadLeaguesAsync();
        await Matches.SearchAsync();
        await Analytics.RefreshAllAsync();
    }

    private void OpenTeamDetails(int teamApiId)
    {
        TeamDetails.SelectedTeamApiId = teamApiId;
        TeamDetails.LoadCommand.Execute(null);
        SelectedTab = AppTab.TeamDetails;
    }

    private void OpenPlayerDetails(int playerApiId)
    {
        PlayerDetails.SelectedPlayerApiId = playerApiId;
        PlayerDetails.LoadCommand.Execute(null);
        SelectedTab = AppTab.PlayerDetails;
    }

    private void OpenMatchDetails(int matchApiId)
    {
        MatchDetails.SelectedMatchApiId = matchApiId;
        MatchDetails.LoadCommand.Execute(null);
        SelectedTab = AppTab.MatchDetails;
    }

    private void OnBackgroundCommandException(Exception exception)
    {
        _logger.LogError(exception, "Main view model command failed.");
    }

    public void Dispose()
    {
        CountriesLeagues.Dispose();
    }
}
