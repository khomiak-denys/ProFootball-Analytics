using Microsoft.Extensions.Logging;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Presentation.Commands;
using ProFootball.Presentation.Services;

namespace ProFootball.Presentation.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private AppTab _selectedTab = AppTab.Dashboard;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IThemeService _themeService;

    public MainViewModel(
        IQueryDispatcher queryDispatcher,
        IThemeService themeService,
        ILoggerFactory loggerFactory)
    {
        _themeService = themeService;
        _logger = loggerFactory.CreateLogger<MainViewModel>();
        Dashboard = new DashboardViewModel(queryDispatcher);
        CountriesLeagues = new CountriesLeaguesViewModel(
            queryDispatcher,
            loggerFactory.CreateLogger<CountriesLeaguesViewModel>());
        TeamDetails = new TeamDetailsViewModel(queryDispatcher);
        PlayerDetails = new PlayerDetailsViewModel(queryDispatcher);
        MatchDetails = new MatchDetailsViewModel(queryDispatcher);

        Teams = new TeamsViewModel(queryDispatcher, OpenTeamDetails);
        Players = new PlayersViewModel(queryDispatcher, OpenPlayerDetails);
        Matches = new MatchesViewModel(queryDispatcher, OpenMatchDetails);
        Analytics = new AnalyticsViewModel(queryDispatcher);

        LoadInitialDataCommand = new AsyncRelayCommand(LoadInitialDataAsync, OnBackgroundCommandException);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
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
        AppTab.CountriesLeagues => "Leagues & Countries",
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

    public RelayCommand ToggleThemeCommand { get; }

    public bool IsDarkTheme => _themeService.CurrentTheme == AppThemeMode.Dark;

    public string ThemeToggleLabel => IsDarkTheme ? "Light" : "Dark";

    public string ThemeToggleGlyph => IsDarkTheme ? "\uE706" : "\uE708";

    public async Task LoadInitialDataAsync()
    {
        await Dashboard.RefreshAsync();
        await CountriesLeagues.RefreshAsync();
        await Teams.SearchAsync();
        await Players.SearchAsync();
        await Matches.LoadLeaguesAsync();
        await Matches.LoadTeamOptionsAsync();
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

    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
        RaisePropertyChanged(nameof(IsDarkTheme));
        RaisePropertyChanged(nameof(ThemeToggleLabel));
        RaisePropertyChanged(nameof(ThemeToggleGlyph));
    }

    public void Dispose()
    {
        Analytics.Dispose();
        Teams.Dispose();
        Players.Dispose();
        CountriesLeagues.Dispose();
    }
}
