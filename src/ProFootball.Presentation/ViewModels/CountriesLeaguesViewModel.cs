using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class CountriesLeaguesViewModel : ObservableObject, IDisposable
{
    private readonly ICountriesLeaguesQueryService _service;
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

    public CountriesLeaguesViewModel(
        ICountriesLeaguesQueryService service,
        ILogger<CountriesLeaguesViewModel> logger)
    {
        _service = service;
        _logger = logger;
        Countries = new ObservableCollection<CountryLeagueListItemDto>();
        LeagueCards = new ObservableCollection<LeagueCountryCardDto>();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, CommandExceptionHandler.Handle);
    }

    public ObservableCollection<CountryLeagueListItemDto> Countries { get; }

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
            RaisePropertyChanged(nameof(SelectedCountryDescription));
            _ = ReloadCountrySnapshotSafeAsync();
        }
    }

    public AsyncRelayCommand RefreshCommand { get; }

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

    public string SelectedCountryDescription => BuildCountryDescription(SelectedCountry?.Name);

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

            var countries = await _service.GetCountriesWithLeagueCountAsync();
            Countries.Clear();
            foreach (var country in countries)
            {
                Countries.Add(country);
            }

            RaisePropertyChanged(nameof(TotalCountries));
            RaisePropertyChanged(nameof(TotalLeagues));

            var previouslySelectedCountryId = SelectedCountry?.Id;
            var nextSelection = countries.FirstOrDefault(country => country.Id == previouslySelectedCountryId)
                ?? countries.FirstOrDefault();

            if (!ReferenceEquals(_selectedCountry, nextSelection))
            {
                SelectedCountry = nextSelection;
            }
            else
            {
                await ReloadCountrySnapshotSafeAsync();
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

            var selectedCountryId = SelectedCountry?.Id;
            if (!selectedCountryId.HasValue)
            {
                ClearCountrySnapshot();
                return;
            }

            var snapshot = await _service.GetCountrySnapshotAsync(selectedCountryId.Value, reloadToken);
            var cards = snapshot.LeagueCards;
            var summary = snapshot.Summary;

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
            _logger.LogError(exception, "Failed to load country snapshot for country {CountryId}.", SelectedCountry?.Id);
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

    public void Dispose()
    {
        CancelSource(_reloadCts);
        DisposeSource(_reloadCts);
        _reloadCts = null;
        _reloadLock.Dispose();
    }
}
