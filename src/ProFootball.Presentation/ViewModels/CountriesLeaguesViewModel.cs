using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Country.Dtos;
using ProFootball.Application.Country.Queries;
using ProFootball.Application.Common;
using ProFootball.Application.League.Commands;
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
    private readonly ICommandDispatcher _commandDispatcher;
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
    private string? _featuredLeagueName;
    private string? _featuredLeagueSeason;
    private bool _isUpdatingFeaturedSeasonOptions;
    private string? _selectedFeaturedSeason;
    private bool _isAddLeagueModalOpen;
    private string _newLeagueName = string.Empty;
    private int? _newLeagueMaxTeams;
    private string? _newLeagueDescription;
    private CountryLeagueListItemDto? _selectedCountryForNewLeague;

    public CountriesLeaguesViewModel(
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher,
        ILogger<CountriesLeaguesViewModel> logger)
    {
        _queryDispatcher = queryDispatcher;
        _commandDispatcher = commandDispatcher;
        _logger = logger;
        Countries = new ObservableCollection<CountryLeagueListItemDto>();
        CountryOptions = new ObservableCollection<CountryLeagueListItemDto>();
        LeagueCards = new ObservableCollection<LeagueCountryCardDto>();
        FeaturedLeagueStandings = new ObservableCollection<LeagueStandingRowDto>();
        FeaturedLeagueSeasons = new ObservableCollection<string>();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, CommandExceptionHandler.Handle);
        OpenAddLeagueModalCommand = new RelayCommand(OpenAddLeagueModal);
        CloseAddLeagueModalCommand = new RelayCommand(CloseAddLeagueModal);
        AddLeagueCommand = new AsyncRelayCommand(AddLeagueAsync, CommandExceptionHandler.Handle);
    }

    public ObservableCollection<CountryLeagueListItemDto> Countries { get; }
    public ObservableCollection<CountryLeagueListItemDto> CountryOptions { get; }

    public ObservableCollection<LeagueCountryCardDto> LeagueCards { get; }
    public ObservableCollection<LeagueStandingRowDto> FeaturedLeagueStandings { get; }
    public ObservableCollection<string> FeaturedLeagueSeasons { get; }

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
            RaisePropertyChanged(nameof(SelectedCountryFlag));
            RaisePropertyChanged(nameof(SelectedCountryAboutTitle));
            RaisePropertyChanged(nameof(SelectedCountryDescription));
            RaisePropertyChanged(nameof(EffectiveCountryDescription));
            _ = ReloadCountrySnapshotSafeAsync();
        }
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand OpenAddLeagueModalCommand { get; }
    public RelayCommand CloseAddLeagueModalCommand { get; }
    public AsyncRelayCommand AddLeagueCommand { get; }

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
    public string SelectedCountryFlag => ResolveCountryFlag(SelectedCountry?.Name);

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

    public int? NewLeagueMaxTeams
    {
        get => _newLeagueMaxTeams;
        set => SetProperty(ref _newLeagueMaxTeams, value);
    }

    public string? NewLeagueDescription
    {
        get => _newLeagueDescription;
        set => SetProperty(ref _newLeagueDescription, value);
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
    public string LeagueNamesLine =>
        LeagueCards.Count == 0
            ? "No leagues found"
            : string.Join(", ", LeagueCards.Select(card => card.LeagueName));

    public string FeaturedLeagueName
    {
        get => _featuredLeagueName ?? "--";
        set => SetProperty(ref _featuredLeagueName, value);
    }

    public string FeaturedLeagueSeason
    {
        get => _featuredLeagueSeason ?? "--";
        set => SetProperty(ref _featuredLeagueSeason, value);
    }

    public string? SelectedFeaturedSeason
    {
        get => _selectedFeaturedSeason;
        set
        {
            if (!SetProperty(ref _selectedFeaturedSeason, value))
            {
                return;
            }

            if (_isUpdatingFeaturedSeasonOptions)
            {
                return;
            }

            _ = ReloadCountrySnapshotSafeAsync();
        }
    }

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
                new GetCountrySnapshotQuery(selectedCountryName, SelectedFeaturedSeason),
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
            RaisePropertyChanged(nameof(LeagueNamesLine));

            TotalClubs = summary.TotalClubs;
            ActiveLeagues = summary.ActiveLeagues;
            Divisions = summary.Divisions;
            FeaturedLeagueName = snapshot.FeaturedLeagueName ?? "--";
            FeaturedLeagueSeason = snapshot.FeaturedLeagueSeason ?? "--";
            _isUpdatingFeaturedSeasonOptions = true;
            try
            {
                FeaturedLeagueSeasons.Clear();
                foreach (var season in snapshot.FeaturedLeagueSeasons)
                {
                    FeaturedLeagueSeasons.Add(season);
                }

                SelectedFeaturedSeason = FeaturedLeagueSeasons.FirstOrDefault(season =>
                    string.Equals(season, snapshot.FeaturedLeagueSeason, StringComparison.Ordinal))
                    ?? FeaturedLeagueSeasons.FirstOrDefault();
            }
            finally
            {
                _isUpdatingFeaturedSeasonOptions = false;
            }

            FeaturedLeagueStandings.Clear();
            foreach (var row in snapshot.FeaturedLeagueStandings)
            {
                FeaturedLeagueStandings.Add(row);
            }
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
        RaisePropertyChanged(nameof(LeagueNamesLine));
        TotalClubs = 0;
        ActiveLeagues = 0;
        Divisions = 0;
        FeaturedLeagueName = "--";
        FeaturedLeagueSeason = "--";
        FeaturedLeagueSeasons.Clear();
        SelectedFeaturedSeason = null;
        FeaturedLeagueStandings.Clear();
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
        NewLeagueMaxTeams = null;
        NewLeagueDescription = string.Empty;
        SelectedCountryForNewLeague = SelectedCountry ?? CountryOptions.FirstOrDefault();
        IsAddLeagueModalOpen = true;
    }

    private void CloseAddLeagueModal()
    {
        IsAddLeagueModalOpen = false;
    }

    private async Task AddLeagueAsync()
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

        var result = await _commandDispatcher.DispatchAsync<CreateLeagueCommand, Result>(
            new CreateLeagueCommand(
                SelectedCountryForNewLeague.Name,
                NewLeagueName.Trim(),
                NewLeagueMaxTeams,
                string.IsNullOrWhiteSpace(NewLeagueDescription) ? null : NewLeagueDescription.Trim()));
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        var createdCountry = SelectedCountryForNewLeague.Name;
        await RefreshAsync();
        SelectedCountry = Countries.FirstOrDefault(country =>
            string.Equals(country.Name, createdCountry, StringComparison.OrdinalIgnoreCase));
        ErrorMessage = null;
        IsAddLeagueModalOpen = false;
    }

    private static string ResolveCountryFlag(string? countryName)
    {
        if (string.IsNullOrWhiteSpace(countryName))
        {
            return "\u2691";
        }

        var code = countryName.Trim().ToLowerInvariant() switch
        {
            "england" => "GB",
            "scotland" => "GB",
            "wales" => "GB",
            "ireland" => "IE",
            "northern ireland" => "GB",
            "belgium" => "BE",
            "france" => "FR",
            "germany" => "DE",
            "italy" => "IT",
            "spain" => "ES",
            "netherlands" => "NL",
            "portugal" => "PT",
            "switzerland" => "CH",
            "ukraine" => "UA",
            "tunisia" => "TN",
            "turkey" => "TR",
            "united states" => "US",
            "brazil" => "BR",
            "argentina" => "AR",
            "japan" => "JP",
            _ => null
        };

        return code is null ? "\u2691" : ToFlagEmoji(code);
    }

    private static string ToFlagEmoji(string isoCode)
    {
        var upper = isoCode.ToUpperInvariant();
        if (upper.Length != 2 || !upper.All(c => c is >= 'A' and <= 'Z'))
        {
            return "\u2691";
        }

        var first = char.ConvertFromUtf32(0x1F1E6 + (upper[0] - 'A'));
        var second = char.ConvertFromUtf32(0x1F1E6 + (upper[1] - 'A'));
        return first + second;
    }

    public void Dispose()
    {
        CancelSource(_reloadCts);
        DisposeSource(_reloadCts);
        _reloadCts = null;
        _reloadLock.Dispose();
    }
}
