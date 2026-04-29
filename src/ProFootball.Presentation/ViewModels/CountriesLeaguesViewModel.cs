using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Country.Dtos;
using ProFootball.Application.Country.Queries;
using ProFootball.Application.League.Dtos;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class CountriesLeaguesViewModel : ObservableObject, IDisposable
{
    private static readonly string[] CountryFallbackPool =
    [
        "Albania", "Algeria", "Argentina", "Armenia", "Australia", "Austria", "Azerbaijan", "Bahrain", "Bangladesh", "Belarus",
        "Belgium", "Bolivia", "Bosnia and Herzegovina", "Brazil", "Bulgaria", "Cameroon", "Canada", "Chile", "China", "Colombia",
        "Costa Rica", "Croatia", "Cyprus", "Czech Republic", "Denmark", "Ecuador", "Egypt", "England", "Estonia", "Finland",
        "France", "Georgia", "Germany", "Ghana", "Greece", "Hungary", "Iceland", "India", "Indonesia", "Iran",
        "Iraq", "Ireland", "Israel", "Italy", "Ivory Coast", "Jamaica", "Japan", "Jordan", "Kazakhstan", "Kenya",
        "Kuwait", "Latvia", "Lebanon", "Lithuania", "Luxembourg", "Malaysia", "Mexico", "Moldova", "Montenegro", "Morocco",
        "Netherlands", "New Zealand", "Nigeria", "North Macedonia", "Norway", "Oman", "Pakistan", "Panama", "Paraguay", "Peru",
        "Philippines", "Poland", "Portugal", "Qatar", "Romania", "Saudi Arabia", "Scotland", "Senegal", "Serbia", "Singapore",
        "Slovakia", "Slovenia", "South Africa", "South Korea", "Spain", "Sweden", "Switzerland", "Syria", "Thailand", "Tunisia",
        "Turkey", "Ukraine", "United Arab Emirates", "United States", "Uruguay", "Uzbekistan", "Venezuela", "Vietnam", "Wales", "Zimbabwe"
    ];

    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ILogger<CountriesLeaguesViewModel> _logger;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private CancellationTokenSource? _reloadCts;
    private CountryLeagueListItemDto? _selectedCountry;
    private bool _isLoading;
    private string? _errorMessage;
    private long _lastLoadingOperationId;
    private int _totalClubs;
    private int _activeLeagues;
    private int _divisions;
    private string? _snapshotAboutDescription;
    private bool _isAddLeagueModalOpen;
    private string _newLeagueName = string.Empty;
    private CountryLeagueListItemDto? _selectedCountryForNewLeague;
    private int _localLeagueIdSeed = -1;

    public CountriesLeaguesViewModel(
        IQueryDispatcher queryDispatcher,
        ILogger<CountriesLeaguesViewModel> logger)
    {
        _queryDispatcher = queryDispatcher;
        _logger = logger;
        Countries = new ObservableCollection<CountryLeagueListItemDto>();
        CountryOptions = new ObservableCollection<CountryLeagueListItemDto>();
        LeagueCards = new ObservableCollection<LeagueCountryCardDto>();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, CommandExceptionHandler.Handle);
        OpenAddLeagueModalCommand = new RelayCommand(OpenAddLeagueModal);
        CloseAddLeagueModalCommand = new RelayCommand(CloseAddLeagueModal);
        AddLeagueCommand = new RelayCommand(AddLeague);
    }

    public ObservableCollection<CountryLeagueListItemDto> Countries { get; }
    public ObservableCollection<CountryLeagueListItemDto> CountryOptions { get; }

    public ObservableCollection<LeagueCountryCardDto> LeagueCards { get; }

    public CountryLeagueListItemDto? SelectedCountry
    {
        get => _selectedCountry;
        set
        {
            if (ReferenceEquals(_selectedCountry, value))
            {
                return;
            }

            _selectedCountry = value;
            RaisePropertyChanged(nameof(SelectedCountry));
            RaisePropertyChanged(nameof(SelectedCountryName));
            RaisePropertyChanged(nameof(SelectedCountryAboutTitle));
            RaisePropertyChanged(nameof(SelectedCountryDescription));
            RaisePropertyChanged(nameof(EffectiveCountryDescription));
            _ = ReloadCountrySnapshotSafeAsync();
        }
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand OpenAddLeagueModalCommand { get; }
    public RelayCommand CloseAddLeagueModalCommand { get; }
    public RelayCommand AddLeagueCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string SelectedCountryName => SelectedCountry?.Name ?? "Select country";

    public bool IsAddLeagueModalOpen
    {
        get => _isAddLeagueModalOpen;
        private set => SetProperty(ref _isAddLeagueModalOpen, value);
    }

    public string NewLeagueName
    {
        get => _newLeagueName;
        set => SetProperty(ref _newLeagueName, value);
    }

    public CountryLeagueListItemDto? SelectedCountryForNewLeague
    {
        get => _selectedCountryForNewLeague;
        set => SetProperty(ref _selectedCountryForNewLeague, value);
    }

    public string SelectedCountryAboutTitle => SelectedCountry is null
        ? "Country football overview"
        : $"About {SelectedCountry.Name} Football";

    public string SelectedCountryDescription => BuildCountryDescription(SelectedCountry?.Name);
    public string EffectiveCountryDescription =>
        !string.IsNullOrWhiteSpace(_snapshotAboutDescription)
            ? _snapshotAboutDescription
            : BuildCountryDescription(SelectedCountry?.Name);

    public int TotalCountries => Countries.Count;

    public int TotalLeagues => Countries.Sum(country => country.LeagueCount);

    public int TotalClubs
    {
        get => _totalClubs;
        private set => SetProperty(ref _totalClubs, value);
    }

    public int ActiveLeagues
    {
        get => _activeLeagues;
        private set => SetProperty(ref _activeLeagues, value);
    }

    public int Divisions
    {
        get => _divisions;
        private set => SetProperty(ref _divisions, value);
    }

    public async Task RefreshAsync()
    {
        var loadingOperationId = BeginLoading();
        try
        {
            ErrorMessage = null;

            var countries = await _queryDispatcher.DispatchAsync<GetCountriesWithLeagueCountQuery, IReadOnlyList<CountryLeagueListItemDto>>(
                new GetCountriesWithLeagueCountQuery());
            var countriesWithLeagues = countries
                .Where(country => country.LeagueCount > 0)
                .ToList();
            Countries.Clear();
            foreach (var country in countriesWithLeagues)
            {
                Countries.Add(country);
            }

            RebuildCountryOptions(countriesWithLeagues);

            RaisePropertyChanged(nameof(TotalCountries));
            RaisePropertyChanged(nameof(TotalLeagues));

            var previouslySelectedCountryName = SelectedCountry?.Name;
            var nextSelection = countriesWithLeagues.FirstOrDefault(country =>
                    string.Equals(country.Name, previouslySelectedCountryName, StringComparison.Ordinal))
                ?? countriesWithLeagues.FirstOrDefault();

            if (!ReferenceEquals(_selectedCountry, nextSelection))
            {
                SelectedCountry = nextSelection;
            }
            else
            {
                await ReloadCountrySnapshotSafeAsync();
            }

            if (SelectedCountryForNewLeague is null)
            {
                SelectedCountryForNewLeague = SelectedCountry ?? CountryOptions.FirstOrDefault();
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load countries.");
            ErrorMessage = "Failed to load countries and leagues.";
        }
        finally
        {
            EndLoading(loadingOperationId);
        }
    }

    private async Task ReloadCountrySnapshotSafeAsync()
    {
        var loadingOperationId = BeginLoading();
        var (currentSource, previousSource) = ReplaceReloadSource();
        var reloadToken = currentSource.Token;
        var lockAcquired = false;

        try
        {
            await _reloadLock.WaitAsync(reloadToken);
            lockAcquired = true;

            ErrorMessage = null;

            var selectedCountryName = SelectedCountry?.Name;
            if (string.IsNullOrWhiteSpace(selectedCountryName))
            {
                ClearCountrySnapshot();
                return;
            }

            var snapshot = await _queryDispatcher.DispatchAsync<GetCountrySnapshotQuery, CountryLeagueSnapshotDto>(
                new GetCountrySnapshotQuery(selectedCountryName),
                reloadToken);
            var cards = snapshot.LeagueCards;
            var summary = snapshot.Summary;
            _snapshotAboutDescription = snapshot.AboutDescription;
            RaisePropertyChanged(nameof(EffectiveCountryDescription));

            LeagueCards.Clear();
            foreach (var card in cards)
            {
                LeagueCards.Add(card);
            }

            TotalClubs = summary.TotalClubs;
            ActiveLeagues = summary.ActiveLeagues;
            Divisions = summary.Divisions;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load country snapshot for country {CountryName}.", SelectedCountry?.Name);
            ErrorMessage = "Failed to load leagues data.";
            ClearCountrySnapshot();
        }
        finally
        {
            if (lockAcquired)
            {
                _reloadLock.Release();
            }

            if (ReferenceEquals(_reloadCts, currentSource))
            {
                _reloadCts = null;
            }

            DisposeSource(previousSource);
            DisposeSource(currentSource);
            EndLoading(loadingOperationId);
        }
    }

    private void ClearCountrySnapshot()
    {
        LeagueCards.Clear();
        TotalClubs = 0;
        ActiveLeagues = 0;
        Divisions = 0;
        _snapshotAboutDescription = null;
        RaisePropertyChanged(nameof(EffectiveCountryDescription));
    }

    private long BeginLoading()
    {
        var operationId = Interlocked.Increment(ref _lastLoadingOperationId);
        IsLoading = true;
        return operationId;
    }

    private void EndLoading(long operationId)
    {
        if (operationId == Volatile.Read(ref _lastLoadingOperationId))
        {
            IsLoading = false;
        }
    }

    private (CancellationTokenSource Current, CancellationTokenSource? Previous) ReplaceReloadSource()
    {
        var newSource = new CancellationTokenSource();
        var previousSource = Interlocked.Exchange(ref _reloadCts, newSource);
        CancelSource(previousSource);
        return (newSource, previousSource);
    }

    private static void CancelSource(CancellationTokenSource? source)
    {
        if (source is null)
        {
            return;
        }

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void DisposeSource(CancellationTokenSource? source)
    {
        if (source is null)
        {
            return;
        }

        try
        {
            source.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static string BuildCountryDescription(string? countryName)
    {
        if (string.IsNullOrWhiteSpace(countryName))
        {
            return "Select a country to view league information and structure highlights.";
        }

        return $"{countryName} has a competitive football ecosystem with multiple divisions. " +
               "League structure snapshots below summarize active competitions, club participation, and latest-season activity.";
    }

    private void RebuildCountryOptions(IReadOnlyCollection<CountryLeagueListItemDto> sourceCountries)
    {
        CountryOptions.Clear();
        foreach (var country in sourceCountries)
        {
            CountryOptions.Add(country);
        }

        var existing = CountryOptions
            .Select(country => country.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var countryName in CountryFallbackPool)
        {
            if (CountryOptions.Count >= 100)
            {
                break;
            }

            if (existing.Add(countryName))
            {
                CountryOptions.Add(new CountryLeagueListItemDto(countryName, 0));
            }
        }
    }

    private void OpenAddLeagueModal()
    {
        ErrorMessage = null;
        NewLeagueName = string.Empty;
        SelectedCountryForNewLeague = SelectedCountry ?? CountryOptions.FirstOrDefault();
        IsAddLeagueModalOpen = true;
    }

    private void CloseAddLeagueModal()
    {
        IsAddLeagueModalOpen = false;
    }

    private void AddLeague()
    {
        if (string.IsNullOrWhiteSpace(NewLeagueName))
        {
            ErrorMessage = "League name is required.";
            return;
        }

        if (SelectedCountryForNewLeague is null)
        {
            ErrorMessage = "Country is required.";
            return;
        }

        var normalizedLeagueName = NewLeagueName.Trim();
        var season = LeagueCards.Select(card => card.Season).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "2025/26";
        var countryName = SelectedCountryForNewLeague.Name;
        LeagueCards.Insert(0, new LeagueCountryCardDto(
            _localLeagueIdSeed--,
            normalizedLeagueName,
            season,
            0,
            0,
            null,
            null));

        UpsertCountryAfterLeagueAdd(countryName);

        ErrorMessage = null;
        IsAddLeagueModalOpen = false;
    }

    private void UpsertCountryAfterLeagueAdd(string countryName)
    {
        var existingCountry = Countries.FirstOrDefault(country =>
            string.Equals(country.Name, countryName, StringComparison.OrdinalIgnoreCase));

        if (existingCountry is null)
        {
            var addedCountry = new CountryLeagueListItemDto(countryName, 1);
            Countries.Add(addedCountry);
            SelectedCountry = addedCountry;
        }
        else
        {
            var countryIndex = Countries.IndexOf(existingCountry);
            Countries[countryIndex] = existingCountry with { LeagueCount = existingCountry.LeagueCount + 1 };
        }

        var optionCountry = CountryOptions.FirstOrDefault(country =>
            string.Equals(country.Name, countryName, StringComparison.OrdinalIgnoreCase));
        if (optionCountry is not null)
        {
            var optionIndex = CountryOptions.IndexOf(optionCountry);
            CountryOptions[optionIndex] = optionCountry with { LeagueCount = optionCountry.LeagueCount + 1 };
            SelectedCountryForNewLeague = CountryOptions[optionIndex];
        }

        RaisePropertyChanged(nameof(TotalCountries));
        RaisePropertyChanged(nameof(TotalLeagues));
    }

    public void Dispose()
    {
        CancelSource(_reloadCts);
        DisposeSource(_reloadCts);
        _reloadCts = null;
        _reloadLock.Dispose();
    }
}
