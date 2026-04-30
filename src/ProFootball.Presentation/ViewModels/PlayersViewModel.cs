using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Common;
using ProFootball.Application.Player.Commands;
using ProFootball.Application.Player.Dtos;
using ProFootball.Application.Player.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class PlayersViewModel : ObservableObject, IDisposable
{
    private static readonly string[] PositionCycle =
    [
        "GK",
        "CB",
        "LB",
        "RB",
        "CDM",
        "CM",
        "CAM",
        "LW",
        "RW",
        "ST",
    ];

    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly Action<int> _openDetails;
    private readonly DispatcherTimer _nameSearchDebounceTimer;
    private readonly IReadOnlyList<string> _overallRatingOptions;
    private readonly IReadOnlyList<string> _preferredFootOptions;

    private PlayerListEntryViewModel? _selectedPlayer;
    private PlayerDetailsDto? _selectedPlayerDetails;
    private string? _nameFilter;
    private string _selectedOverallRatingOption;
    private string _selectedPreferredFootOption;
    private bool _isOverviewSelected = true;
    private bool _isLoadingList;
    private bool _isLoadingDetails;
    private string? _errorMessage;
    private int _page = 1;
    private int _pageSize = 20;
    private int _totalCount;
    private string _radarPolygonPoints = string.Empty;
    private long _searchVersion;
    private long _detailsVersion;
    private string _editorFirstName = string.Empty;
    private string _editorLastName = string.Empty;
    private DateTime? _editorBirthday;
    private int? _editorHeight;
    private int? _editorWeight;
    private bool _isAddPlayerModalOpen;
    private string _newPlayerFirstName = string.Empty;
    private string _newPlayerLastName = string.Empty;
    private DateTime? _newPlayerBirthday;
    private int? _newPlayerHeight;
    private int? _newPlayerWeight;
    private bool _isEditPlayerModalOpen;

    public PlayersViewModel(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher, Action<int> openDetails)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _openDetails = openDetails;

        _overallRatingOptions = ["All Ratings", "90+", "85+", "80+", "75+"];
        _preferredFootOptions = ["All", "Right", "Left"];

        _selectedOverallRatingOption = _overallRatingOptions[0];
        _selectedPreferredFootOption = _preferredFootOptions[0];

        _nameSearchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300),
        };
        _nameSearchDebounceTimer.Tick += OnNameSearchDebounceTimerTick;

        Players = new ObservableCollection<PlayerListEntryViewModel>();
        SelectedPlayerHistory = new ObservableCollection<PlayerAttributeDto>();

        SearchCommand = new AsyncRelayCommand(StartSearchAsync, CommandExceptionHandler.Handle);
        OpenDetailsCommand = new RelayCommand(OpenDetails, () => SelectedPlayer is not null);
        ShowOverviewCommand = new RelayCommand(ShowOverview);
        ShowHistoryCommand = new RelayCommand(ShowHistory);
        CreatePlayerCommand = new AsyncRelayCommand(CreatePlayerAsync, CommandExceptionHandler.Handle);
        UpdatePlayerCommand = new AsyncRelayCommand(UpdatePlayerAsync, CommandExceptionHandler.Handle, () => SelectedPlayer is not null);
        DeletePlayerCommand = new AsyncRelayCommand(DeletePlayerAsync, CommandExceptionHandler.Handle, () => SelectedPlayer is not null);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, CommandExceptionHandler.Handle, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, CommandExceptionHandler.Handle, () => HasPreviousPage);
        OpenAddPlayerModalCommand = new RelayCommand(OpenAddPlayerModal);
        CloseAddPlayerModalCommand = new RelayCommand(CloseAddPlayerModal);
        AddPlayerCommand = new AsyncRelayCommand(AddPlayerAsync, CommandExceptionHandler.Handle);
        OpenEditPlayerModalCommand = new RelayCommand(OpenEditPlayerModal, () => SelectedPlayer is not null);
        CloseEditPlayerModalCommand = new RelayCommand(CloseEditPlayerModal);
        SaveEditPlayerCommand = new AsyncRelayCommand(SaveEditPlayerAsync, CommandExceptionHandler.Handle, () => SelectedPlayer is not null);
    }

    public ObservableCollection<PlayerListEntryViewModel> Players { get; }

    public ObservableCollection<PlayerAttributeDto> SelectedPlayerHistory { get; }

    public IReadOnlyList<PlayerTrendPointDto> SelectedPlayerTrendPoints =>
        SelectedPlayerHistory
            .OrderBy(attribute => attribute.Date)
            .Select(attribute => new PlayerTrendPointDto(attribute.Date, attribute.OverallRating, attribute.Potential))
            .ToList();

    public IReadOnlyList<string> OverallRatingOptions => _overallRatingOptions;

    public IReadOnlyList<string> PreferredFootOptions => _preferredFootOptions;

    public PlayerListEntryViewModel? SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            if (!SetProperty(ref _selectedPlayer, value))
            {
                return;
            }

            OpenDetailsCommand.RaiseCanExecuteChanged();
            UpdatePlayerCommand.RaiseCanExecuteChanged();
            DeletePlayerCommand.RaiseCanExecuteChanged();
            OpenEditPlayerModalCommand.RaiseCanExecuteChanged();
            SaveEditPlayerCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(HasSelectedPlayer));
            RaiseSelectedPlayerPropertiesChanged();
            SyncEditorWithSelection();
            _ = LoadSelectedPlayerDetailsSafeAsync();
        }
    }

    public bool HasSelectedPlayer => SelectedPlayer is not null;

    public string? NameFilter
    {
        get => _nameFilter;
        set
        {
            if (!SetProperty(ref _nameFilter, value))
            {
                return;
            }

            ScheduleDebouncedSearch();
        }
    }

    public string SelectedOverallRatingOption
    {
        get => _selectedOverallRatingOption;
        set
        {
            if (!SetProperty(ref _selectedOverallRatingOption, value))
            {
                return;
            }

            _ = StartSearchSafeAsync();
        }
    }

    public string SelectedPreferredFootOption
    {
        get => _selectedPreferredFootOption;
        set
        {
            if (!SetProperty(ref _selectedPreferredFootOption, value))
            {
                return;
            }

            _ = StartSearchSafeAsync();
        }
    }

    public bool IsOverviewSelected
    {
        get => _isOverviewSelected;
        private set
        {
            if (SetProperty(ref _isOverviewSelected, value))
            {
                RaisePropertyChanged(nameof(IsHistorySelected));
            }
        }
    }

    public bool IsHistorySelected => !IsOverviewSelected;

    public bool IsLoadingList
    {
        get => _isLoadingList;
        private set => SetProperty(ref _isLoadingList, value);
    }

    public bool IsLoadingDetails
    {
        get => _isLoadingDetails;
        private set => SetProperty(ref _isLoadingDetails, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public int Page
    {
        get => _page;
        private set
        {
            if (SetProperty(ref _page, value))
            {
                RaisePropertyChanged(nameof(HasPreviousPage));
                RaisePropertyChanged(nameof(HasNextPage));
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                RaisePropertyChanged(nameof(HasPreviousPage));
                RaisePropertyChanged(nameof(HasNextPage));
                NextPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
                _ = StartSearchSafeAsync();
            }
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        private set
        {
            if (SetProperty(ref _totalCount, value))
            {
                RaisePropertyChanged(nameof(HasNextPage));
            }
        }
    }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page * PageSize < TotalCount;

    public string DetailsPlayerName => _selectedPlayerDetails is null
        ? SelectedPlayer?.FullName ?? "Select a player"
        : string.Create(CultureInfo.InvariantCulture, $"{_selectedPlayerDetails.FirstName} {_selectedPlayerDetails.LastName}").Trim();

    public string DetailsPosition => SelectedPlayer is null ? "--" : ResolvePosition(SelectedPlayer.PlayerApiId);

    public string DetailsOverall => FormatInt(GetLatestOverallRating() ?? SelectedPlayer?.OverallRating);

    public string DetailsPotential => FormatInt(GetLatestPotential() ?? SelectedPlayer?.Potential);

    public string DetailsAge => FormatInt(SelectedPlayer?.Age);

    public string DetailsFoot => NormalizeText(GetLatestPreferredFoot(), "--") ?? "--";

    public string DetailsHeight => FormatMeasure(SelectedPlayer?.Height, "cm");

    public string DetailsWeight => FormatMeasure(SelectedPlayer?.Weight, "kg");

    public string RadarPolygonPoints
    {
        get => _radarPolygonPoints;
        private set => SetProperty(ref _radarPolygonPoints, value);
    }

    public int PaceMetric => BuildRadarMetrics().Pace;

    public int ShootingMetric => BuildRadarMetrics().Shooting;

    public int PassingMetric => BuildRadarMetrics().Passing;

    public int DribblingMetric => BuildRadarMetrics().Dribbling;

    public int DefendingMetric => BuildRadarMetrics().Defending;

    public int PhysicalMetric => BuildRadarMetrics().Physical;

    public AsyncRelayCommand SearchCommand { get; }

    public RelayCommand OpenDetailsCommand { get; }

    public RelayCommand ShowOverviewCommand { get; }

    public RelayCommand ShowHistoryCommand { get; }

    public AsyncRelayCommand CreatePlayerCommand { get; }

    public AsyncRelayCommand UpdatePlayerCommand { get; }

    public AsyncRelayCommand DeletePlayerCommand { get; }

    public AsyncRelayCommand NextPageCommand { get; }

    public AsyncRelayCommand PreviousPageCommand { get; }

    public RelayCommand OpenAddPlayerModalCommand { get; }

    public RelayCommand CloseAddPlayerModalCommand { get; }

    public AsyncRelayCommand AddPlayerCommand { get; }

    public RelayCommand OpenEditPlayerModalCommand { get; }

    public RelayCommand CloseEditPlayerModalCommand { get; }

    public AsyncRelayCommand SaveEditPlayerCommand { get; }

    public bool IsAddPlayerModalOpen
    {
        get => _isAddPlayerModalOpen;
        private set => SetProperty(ref _isAddPlayerModalOpen, value);
    }

    public string NewPlayerFirstName
    {
        get => _newPlayerFirstName;
        set => SetProperty(ref _newPlayerFirstName, value);
    }

    public string NewPlayerLastName
    {
        get => _newPlayerLastName;
        set => SetProperty(ref _newPlayerLastName, value);
    }

    public DateTime? NewPlayerBirthday
    {
        get => _newPlayerBirthday;
        set => SetProperty(ref _newPlayerBirthday, value);
    }

    public int? NewPlayerHeight
    {
        get => _newPlayerHeight;
        set => SetProperty(ref _newPlayerHeight, value);
    }

    public int? NewPlayerWeight
    {
        get => _newPlayerWeight;
        set => SetProperty(ref _newPlayerWeight, value);
    }

    public bool IsEditPlayerModalOpen
    {
        get => _isEditPlayerModalOpen;
        private set => SetProperty(ref _isEditPlayerModalOpen, value);
    }

    public string EditorFirstName
    {
        get => _editorFirstName;
        set => SetProperty(ref _editorFirstName, value);
    }

    public string EditorLastName
    {
        get => _editorLastName;
        set => SetProperty(ref _editorLastName, value);
    }

    public DateTime? EditorBirthday
    {
        get => _editorBirthday;
        set => SetProperty(ref _editorBirthday, value);
    }

    public int? EditorHeight
    {
        get => _editorHeight;
        set => SetProperty(ref _editorHeight, value);
    }

    public int? EditorWeight
    {
        get => _editorWeight;
        set => SetProperty(ref _editorWeight, value);
    }

    public async Task SearchAsync()
    {
        var searchVersion = Interlocked.Increment(ref _searchVersion);
        IsLoadingList = true;
        ErrorMessage = null;

        try
        {
            var query = new PlayerSearchQuery(
                Name: NormalizeText(NameFilter, null),
                MinOverallRating: ParseMinimumOverallRating(SelectedOverallRatingOption),
                MaxOverallRating: null,
                MinPotential: null,
                MaxPotential: null,
                MinHeight: null,
                MaxHeight: null,
                PreferredFoot: ParsePreferredFoot(SelectedPreferredFootOption),
                SortBy: "overallrating",
                SortDescending: true,
                Page: Page,
                PageSize: Math.Max(20, PageSize));

            var result = await _queryDispatcher.DispatchAsync<PlayerSearchQuery, PagedResult<PlayerListItemDto>>(query);
            if (searchVersion != Volatile.Read(ref _searchVersion))
            {
                return;
            }

            TotalCount = result.TotalCount;

            var selectedPlayerId = SelectedPlayer?.PlayerApiId;
            Players.Clear();
            foreach (var item in result.Items)
            {
                Players.Add(new PlayerListEntryViewModel(
                    item.PlayerApiId,
                    item.FirstName,
                    item.LastName,
                    ResolvePosition(item.PlayerApiId),
                    ResolveAge(item.Birthday, item.PlayerApiId),
                    item.OverallRating,
                    item.Potential,
                    NormalizeText(item.PreferredFoot, "--") ?? "--",
                    item.Height,
                    item.Weight,
                    FormatMeasure(item.Height, "cm")));
            }

            SelectedPlayer = selectedPlayerId.HasValue
                ? Players.FirstOrDefault(item => item.PlayerApiId == selectedPlayerId.Value) ?? Players.FirstOrDefault()
                : Players.FirstOrDefault();

            NextPageCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
        }
        catch (Exception exception)
        {
            if (searchVersion == Volatile.Read(ref _searchVersion))
            {
                ErrorMessage = "Failed to load players.";
            }

            CommandExceptionHandler.Handle(exception);
        }
        finally
        {
            if (searchVersion == Volatile.Read(ref _searchVersion))
            {
                IsLoadingList = false;
            }
        }
    }

    private async Task StartSearchAsync()
    {
        Page = 1;
        await SearchAsync();
    }

    private async Task StartSearchSafeAsync()
    {
        try
        {
            await StartSearchAsync();
        }
        catch (Exception exception)
        {
            CommandExceptionHandler.Handle(exception);
        }
    }

    private async Task LoadSelectedPlayerDetailsSafeAsync()
    {
        var selectedPlayer = SelectedPlayer;
        var detailsVersion = Interlocked.Increment(ref _detailsVersion);

        if (selectedPlayer is null)
        {
            ClearSelectedPlayerDetailsState();
            return;
        }

        IsLoadingDetails = true;
        ErrorMessage = null;
        ClearSelectedPlayerDetailsState();

        try
        {
            var details = await _queryDispatcher.DispatchAsync<GetPlayerDetailsQuery, PlayerDetailsDto?>(
                new GetPlayerDetailsQuery(selectedPlayer.PlayerApiId));
            if (detailsVersion != Volatile.Read(ref _detailsVersion))
            {
                return;
            }

            _selectedPlayerDetails = details;
            SyncEditorWithSelection();
            SelectedPlayerHistory.Clear();
            if (details is not null)
            {
                foreach (var attribute in details.Attributes.Take(120))
                {
                    SelectedPlayerHistory.Add(attribute);
                }
            }

            UpdateRadarPolygonPoints();
            RaiseSelectedPlayerPropertiesChanged();
            RaisePropertyChanged(nameof(SelectedPlayerTrendPoints));
        }
        catch (Exception exception)
        {
            if (detailsVersion == Volatile.Read(ref _detailsVersion))
            {
                ErrorMessage = "Failed to load player details.";
            }

            CommandExceptionHandler.Handle(exception);
        }
        finally
        {
            if (detailsVersion == Volatile.Read(ref _detailsVersion))
            {
                IsLoadingDetails = false;
            }
        }
    }

    private void ClearSelectedPlayerDetailsState()
    {
        _selectedPlayerDetails = null;
        SelectedPlayerHistory.Clear();
        RadarPolygonPoints = string.Empty;
        RaisePropertyChanged(nameof(SelectedPlayerTrendPoints));
        RaiseSelectedPlayerPropertiesChanged();
    }

    private void OnNameSearchDebounceTimerTick(object? sender, EventArgs e)
    {
        _nameSearchDebounceTimer.Stop();
        _ = StartSearchSafeAsync();
    }

    private void ScheduleDebouncedSearch()
    {
        _nameSearchDebounceTimer.Stop();
        _nameSearchDebounceTimer.Start();
    }

    private void ShowOverview()
    {
        IsOverviewSelected = true;
    }

    private void ShowHistory()
    {
        IsOverviewSelected = false;
    }

    private void OpenDetails()
    {
        if (SelectedPlayer is null)
        {
            return;
        }

        _openDetails(SelectedPlayer.PlayerApiId);
    }

    private async Task NextPageAsync()
    {
        if (!HasNextPage)
        {
            return;
        }

        Page++;
        await SearchAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (!HasPreviousPage)
        {
            return;
        }

        Page--;
        await SearchAsync();
    }

    private async Task CreatePlayerAsync()
    {
        var result = await _commandDispatcher.DispatchAsync<CreatePlayerCommand, Result>(
            new CreatePlayerCommand(EditorFirstName, EditorLastName, EditorBirthday, EditorHeight, EditorWeight));
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        ErrorMessage = null;
        await StartSearchAsync();
    }

    private void OpenAddPlayerModal()
    {
        ErrorMessage = null;
        NewPlayerFirstName = string.Empty;
        NewPlayerLastName = string.Empty;
        NewPlayerBirthday = null;
        NewPlayerHeight = null;
        NewPlayerWeight = null;
        IsAddPlayerModalOpen = true;
    }

    private void CloseAddPlayerModal()
    {
        IsAddPlayerModalOpen = false;
    }

    private async Task AddPlayerAsync()
    {
        var result = await _commandDispatcher.DispatchAsync<CreatePlayerCommand, Result>(
            new CreatePlayerCommand(NewPlayerFirstName, NewPlayerLastName, NewPlayerBirthday, NewPlayerHeight, NewPlayerWeight));
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        ErrorMessage = null;
        IsAddPlayerModalOpen = false;
        await StartSearchAsync();
    }

    private void OpenEditPlayerModal()
    {
        if (SelectedPlayer is null)
        {
            return;
        }

        SyncEditorWithSelection();
        IsEditPlayerModalOpen = true;
    }

    private void CloseEditPlayerModal()
    {
        IsEditPlayerModalOpen = false;
    }

    private async Task SaveEditPlayerAsync()
    {
        if (SelectedPlayer is null)
        {
            return;
        }

        var result = await _commandDispatcher.DispatchAsync<UpdatePlayerCommand, Result>(
            new UpdatePlayerCommand(SelectedPlayer.PlayerApiId, EditorFirstName, EditorLastName, EditorBirthday, EditorHeight, EditorWeight));
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        ErrorMessage = null;
        IsEditPlayerModalOpen = false;
        await SearchAsync();
    }

    private async Task UpdatePlayerAsync()
    {
        if (SelectedPlayer is null)
        {
            return;
        }

        var result = await _commandDispatcher.DispatchAsync<UpdatePlayerCommand, Result>(
            new UpdatePlayerCommand(SelectedPlayer.PlayerApiId, EditorFirstName, EditorLastName, EditorBirthday, EditorHeight, EditorWeight));
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        ErrorMessage = null;
        await SearchAsync();
    }

    private async Task DeletePlayerAsync()
    {
        if (SelectedPlayer is null)
        {
            return;
        }

        var result = await _commandDispatcher.DispatchAsync<DeletePlayerCommand, Result>(
            new DeletePlayerCommand(SelectedPlayer.PlayerApiId));
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        ErrorMessage = null;
        await SearchAsync();
    }

    private void SyncEditorWithSelection()
    {
        if (SelectedPlayer is null)
        {
            EditorFirstName = string.Empty;
            EditorLastName = string.Empty;
            EditorBirthday = null;
            EditorHeight = null;
            EditorWeight = null;
            return;
        }

        EditorFirstName = SelectedPlayer.FirstName;
        EditorLastName = SelectedPlayer.LastName;
        EditorBirthday = _selectedPlayerDetails?.Birthday;
        EditorHeight = SelectedPlayer.Height;
        EditorWeight = SelectedPlayer.Weight;
    }

    private void RaiseSelectedPlayerPropertiesChanged()
    {
        RaisePropertyChanged(nameof(DetailsPlayerName));
        RaisePropertyChanged(nameof(DetailsPosition));
        RaisePropertyChanged(nameof(DetailsOverall));
        RaisePropertyChanged(nameof(DetailsPotential));
        RaisePropertyChanged(nameof(DetailsAge));
        RaisePropertyChanged(nameof(DetailsFoot));
        RaisePropertyChanged(nameof(DetailsHeight));
        RaisePropertyChanged(nameof(DetailsWeight));
        RaisePropertyChanged(nameof(PaceMetric));
        RaisePropertyChanged(nameof(ShootingMetric));
        RaisePropertyChanged(nameof(PassingMetric));
        RaisePropertyChanged(nameof(DribblingMetric));
        RaisePropertyChanged(nameof(DefendingMetric));
        RaisePropertyChanged(nameof(PhysicalMetric));
    }

    private int? GetLatestOverallRating() => _selectedPlayerDetails?.Attributes.FirstOrDefault()?.OverallRating;

    private int? GetLatestPotential() => _selectedPlayerDetails?.Attributes.FirstOrDefault()?.Potential;

    private string? GetLatestPreferredFoot() => _selectedPlayerDetails?.Attributes.FirstOrDefault()?.PreferredFoot ?? SelectedPlayer?.PreferredFoot;

    private RadarMetrics BuildRadarMetrics()
    {
        if (SelectedPlayer is null)
        {
            return new RadarMetrics(0, 0, 0, 0, 0, 0);
        }

        var seed = Math.Abs(SelectedPlayer.PlayerApiId);
        var overall = GetLatestOverallRating() ?? SelectedPlayer.OverallRating ?? 72;
        var potential = GetLatestPotential() ?? SelectedPlayer.Potential ?? overall;
        var height = SelectedPlayer.Height ?? 178;
        var weight = SelectedPlayer.Weight ?? 74;

        var pace = ClampMetric((overall + potential) / 2 + Offset(seed, 1));
        var shooting = ClampMetric(overall + Offset(seed, 2));
        var passing = ClampMetric((overall + potential + 5) / 2 + Offset(seed, 3));
        var dribbling = ClampMetric((potential + 3 * overall) / 4 + Offset(seed, 4));
        var defending = ClampMetric((overall + (220 - height) / 3) + Offset(seed, 5));
        var physical = ClampMetric((overall + weight / 2) + Offset(seed, 6));

        return new RadarMetrics(pace, shooting, passing, dribbling, defending, physical);
    }

    private void UpdateRadarPolygonPoints()
    {
        var metrics = BuildRadarMetrics();
        var values = new[]
        {
            metrics.Pace,
            metrics.Shooting,
            metrics.Passing,
            metrics.Dribbling,
            metrics.Defending,
            metrics.Physical,
        };

        const double centerX = 132;
        const double centerY = 152;
        const double maxRadius = 108;

        var coordinates = values
            .Select((value, index) =>
            {
                var angle = (-90 + index * 60) * Math.PI / 180.0;
                var ratio = Math.Clamp(value / 100.0, 0.0, 1.0);
                var x = centerX + Math.Cos(angle) * maxRadius * ratio;
                var y = centerY + Math.Sin(angle) * maxRadius * ratio;
                return string.Create(CultureInfo.InvariantCulture, $"{x:F1},{y:F1}");
            });

        RadarPolygonPoints = string.Join(" ", coordinates);
    }

    private static int? ParseMinimumOverallRating(string selectedOption)
    {
        if (selectedOption.Length < 2 || selectedOption.Equals("All Ratings", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var candidate = selectedOption.TrimEnd('+').Trim();
        return int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string? ParsePreferredFoot(string selectedOption) =>
        selectedOption.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? null
            : selectedOption;

    private static string ResolvePosition(int playerApiId)
    {
        var index = Math.Abs(playerApiId % PositionCycle.Length);
        return PositionCycle[index];
    }

    private static int ResolveFallbackAge(int playerApiId) => 18 + Math.Abs(playerApiId % 21);

    private static int ResolveAge(DateTime? birthday, int playerApiId)
    {
        if (!birthday.HasValue)
        {
            return ResolveFallbackAge(playerApiId);
        }

        var birthdayDate = birthday.Value.Date;
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthdayDate.Year;
        if (birthdayDate > today.AddYears(-age))
        {
            age--;
        }

        return Math.Clamp(age, 15, 60);
    }

    private static string? NormalizeText(string? value, string? defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim();
    }

    private static string FormatInt(int? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "--";

    private static string FormatMeasure(int? value, string unit) => value.HasValue
        ? string.Create(CultureInfo.InvariantCulture, $"{value.Value} {unit}")
        : "--";

    private static int Offset(int seed, int salt) => ((seed * (17 + salt * 3)) % 15) - 7;

    private static int ClampMetric(int value) => Math.Clamp(value, 25, 99);

    public void Dispose()
    {
        _nameSearchDebounceTimer.Stop();
        _nameSearchDebounceTimer.Tick -= OnNameSearchDebounceTimerTick;
    }

    private readonly record struct RadarMetrics(
        int Pace,
        int Shooting,
        int Passing,
        int Dribbling,
        int Defending,
        int Physical);
}

public sealed record PlayerListEntryViewModel(
    int PlayerApiId,
    string FirstName,
    string LastName,
    string Position,
    int Age,
    int? OverallRating,
    int? Potential,
    string PreferredFoot,
    int? Height,
    int? Weight,
    string HeightDisplay)
{
    public string FullName => string.Create(CultureInfo.InvariantCulture, $"{FirstName} {LastName}").Trim();
}
