using Microsoft.Extensions.Logging;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Commands;
using ProFootball.Application.Auth.Dtos;
using ProFootball.Application.Auth.Queries;
using ProFootball.Application.Common;
using ProFootball.Presentation.ViewModels.Auth;
using ProFootball.Presentation.Commands;
using ProFootball.Presentation.Services;

namespace ProFootball.Presentation.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private AppTab _selectedTab = AppTab.Dashboard;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IThemeService _themeService;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private string _currentUserDisplayName = "Unknown";
    private string _currentUserRole = "Analyst";
    private bool _isAuthenticated;
    private bool _isSessionResolved;
    private bool _isUserMenuOpen;

    public MainViewModel(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        IThemeService themeService,
        IAppSettingsService appSettingsService,
        ILoggerFactory loggerFactory)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _themeService = themeService;
        _logger = loggerFactory.CreateLogger<MainViewModel>();
        LoginForm = new LoginViewModel(commandDispatcher);
        RegistrationForm = new RegistrationViewModel(commandDispatcher);
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
        Settings = new SettingsViewModel(themeService, appSettingsService);

        LoadInitialDataCommand = new AsyncRelayCommand(LoadInitialDataAsync, OnBackgroundCommandException);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        ToggleUserMenuCommand = new RelayCommand(ToggleUserMenu, () => IsAuthenticated);
        OpenSettingsCommand = new RelayCommand(OpenSettings, () => IsAuthenticated);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync, OnBackgroundCommandException, () => IsAuthenticated);
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

    public SettingsViewModel Settings { get; }

    public LoginViewModel LoginForm { get; }

    public RegistrationViewModel RegistrationForm { get; }

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
        AppTab.Settings => "Settings",
        _ => "Dashboard",
    };

    public AsyncRelayCommand LoadInitialDataCommand { get; }

    public RelayCommand ToggleThemeCommand { get; }

    public RelayCommand ToggleUserMenuCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    public AsyncRelayCommand LogoutCommand { get; }

    public bool IsDarkTheme => _themeService.CurrentTheme == AppThemeMode.Dark;

    public string ThemeToggleLabel => IsDarkTheme ? "Light" : "Dark";

    public string ThemeToggleGlyph => IsDarkTheme ? "\uE706" : "\uE708";

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set
        {
            if (SetProperty(ref _isAuthenticated, value))
            {
                if (!value)
                {
                    IsUserMenuOpen = false;
                }

                RaisePropertyChanged(nameof(ShowAuthenticatedShell));
                RaisePropertyChanged(nameof(ShowAuthScreen));
                ToggleUserMenuCommand.RaiseCanExecuteChanged();
                OpenSettingsCommand.RaiseCanExecuteChanged();
                LogoutCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsSessionResolved
    {
        get => _isSessionResolved;
        private set
        {
            if (SetProperty(ref _isSessionResolved, value))
            {
                RaisePropertyChanged(nameof(ShowAuthenticatedShell));
                RaisePropertyChanged(nameof(ShowAuthScreen));
            }
        }
    }

    public bool ShowAuthenticatedShell => IsSessionResolved && IsAuthenticated;

    public bool ShowAuthScreen => IsSessionResolved && !IsAuthenticated;

    public bool IsUserMenuOpen
    {
        get => _isUserMenuOpen;
        set => SetProperty(ref _isUserMenuOpen, value);
    }

    public string CurrentUserDisplayName
    {
        get => _currentUserDisplayName;
        private set => SetProperty(ref _currentUserDisplayName, value);
    }

    public string CurrentUserRole
    {
        get => _currentUserRole;
        private set
        {
            if (SetProperty(ref _currentUserRole, value))
            {
                RaisePropertyChanged(nameof(CanManageData));
                RaisePropertyChanged(nameof(IsReadOnlyUser));
            }
        }
    }

    public bool CanManageData =>
        string.Equals(CurrentUserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(CurrentUserRole, "Manager", StringComparison.OrdinalIgnoreCase);

    public bool IsReadOnlyUser => !CanManageData;

    public async Task LoadInitialDataAsync()
    {
        if (!IsSessionResolved)
        {
            await RestorePersistedSessionAsync();
            IsSessionResolved = true;
        }

        await RefreshSessionStateAsync();
        if (!IsAuthenticated)
        {
            return;
        }

        await Task.WhenAll(
            Dashboard.RefreshAsync(),
            CountriesLeagues.RefreshAsync(),
            Teams.SearchAsync(),
            Players.SearchAsync(),
            LoadMatchesAsync(),
            Analytics.RefreshAllAsync());
    }

    private async Task RestorePersistedSessionAsync()
    {
        var result = await _commandDispatcher.DispatchAsync<RestoreSessionCommand, Result>(new RestoreSessionCommand());
        if (result.IsFailure)
        {
            _logger.LogWarning("Session restore failed: {Code} {Message}", result.Error.Code, result.Error.Message);
        }
    }

    private async Task LoadMatchesAsync()
    {
        await Matches.LoadLeaguesAsync();
        await Matches.LoadSeasonOptionsAsync();
        await Matches.LoadTeamOptionsAsync();
        await Matches.SearchAsync();
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

    private void ToggleUserMenu()
    {
        if (!IsAuthenticated)
        {
            IsUserMenuOpen = false;
            return;
        }

        IsUserMenuOpen = !IsUserMenuOpen;
    }

    private void OpenSettings()
    {
        IsUserMenuOpen = false;
        SelectedTab = AppTab.Settings;
    }

    private async Task LogoutAsync()
    {
        IsUserMenuOpen = false;
        await _commandDispatcher.DispatchAsync(new SignOutCommand());
        await RefreshSessionStateAsync();
        SelectedTab = AppTab.Dashboard;
    }

    private async Task RefreshSessionStateAsync()
    {
        SessionStateDto session = await _queryDispatcher.DispatchAsync<GetSessionStateQuery, SessionStateDto>(new GetSessionStateQuery());
        IsAuthenticated = session.IsAuthenticated;
        CurrentUserDisplayName = string.IsNullOrWhiteSpace(session.DisplayName) ? "Guest" : session.DisplayName;
        CurrentUserRole = string.IsNullOrWhiteSpace(session.Role) ? "Analyst" : session.Role;
    }

    public void Dispose()
    {
        Analytics.Dispose();
        Teams.Dispose();
        Players.Dispose();
        CountriesLeagues.Dispose();
    }
}
