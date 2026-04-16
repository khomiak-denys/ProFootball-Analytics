using Microsoft.Extensions.Logging;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    public static class Tabs
    {
        public const int Dashboard = 0;
        public const int CountriesLeagues = 1;
        public const int Teams = 2;
        public const int TeamDetails = 3;
        public const int Players = 4;
        public const int PlayerDetails = 5;
        public const int Matches = 6;
        public const int MatchDetails = 7;
        public const int Analytics = 8;
    }

    private int _selectedTabIndex;
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

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                RaisePropertyChanged(nameof(CurrentSectionTitle));
            }
        }
    }

    public string CurrentSectionTitle => SelectedTabIndex switch
    {
        Tabs.Dashboard => "Dashboard",
        Tabs.CountriesLeagues => "Leagues",
        Tabs.Teams => "Teams",
        Tabs.TeamDetails => "Team Details",
        Tabs.Players => "Players",
        Tabs.PlayerDetails => "Player Details",
        Tabs.Matches => "Matches",
        Tabs.MatchDetails => "Match Details",
        Tabs.Analytics => "Analytics",
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
        SelectedTabIndex = Tabs.TeamDetails;
    }

    private void OpenPlayerDetails(int playerApiId)
    {
        PlayerDetails.SelectedPlayerApiId = playerApiId;
        PlayerDetails.LoadCommand.Execute(null);
        SelectedTabIndex = Tabs.PlayerDetails;
    }

    private void OpenMatchDetails(int matchApiId)
    {
        MatchDetails.SelectedMatchApiId = matchApiId;
        MatchDetails.LoadCommand.Execute(null);
        SelectedTabIndex = Tabs.MatchDetails;
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
