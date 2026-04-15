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
    private readonly SemaphoreSlim _loadLeaguesLock = new(1, 1);
    private readonly List<CancellationTokenSource> _retiredLoadCts = [];
    private CancellationTokenSource? _loadLeaguesCts;
    private CountryDto? _selectedCountry;
    private bool _isLoading;
    private string? _errorMessage;
    private long _lastLoadingOperationId;

    public CountriesLeaguesViewModel(
        ICountriesLeaguesQueryService service,
        ILogger<CountriesLeaguesViewModel> logger)
    {
        _service = service;
        _logger = logger;
        Countries = new ObservableCollection<CountryDto>();
        Leagues = new ObservableCollection<LeagueDto>();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, CommandExceptionHandler.Handle);
    }

    public ObservableCollection<CountryDto> Countries { get; }

    public ObservableCollection<LeagueDto> Leagues { get; }

    public CountryDto? SelectedCountry
    {
        get => _selectedCountry;
        set
        {
            if (!SetProperty(ref _selectedCountry, value))
            {
                return;
            }

            _ = ReloadLeaguesSafeAsync();
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

    public async Task RefreshAsync()
    {
        var loadingOperationId = BeginLoading();

        try
        {
            ErrorMessage = null;

            Countries.Clear();
            var countries = await _service.GetCountriesAsync();
            foreach (var country in countries)
            {
                Countries.Add(country);
            }

            await ReloadLeaguesSafeAsync();
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

    private async Task ReloadLeaguesSafeAsync()
    {
        var loadingOperationId = BeginLoading();
        var loadToken = ReplaceLoadToken();
        var lockAcquired = false;

        try
        {
            await _loadLeaguesLock.WaitAsync(loadToken);
            lockAcquired = true;
            DisposeRetiredLoadSources();

            ErrorMessage = null;
            loadToken.ThrowIfCancellationRequested();
            var leagues = await _service.GetLeaguesAsync(SelectedCountry?.Id, loadToken);
            loadToken.ThrowIfCancellationRequested();

            Leagues.Clear();
            foreach (var league in leagues)
            {
                Leagues.Add(league);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load leagues for country {CountryId}.", SelectedCountry?.Id);
            ErrorMessage = "Failed to load leagues.";
        }
        finally
        {
            if (lockAcquired)
            {
                _loadLeaguesLock.Release();
            }

            EndLoading(loadingOperationId);
        }
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

    private CancellationToken ReplaceLoadToken()
    {
        var previousSource = _loadLeaguesCts;
        previousSource?.Cancel();
        if (previousSource is not null)
        {
            _retiredLoadCts.Add(previousSource);
        }

        _loadLeaguesCts = new CancellationTokenSource();
        return _loadLeaguesCts.Token;
    }

    private void DisposeRetiredLoadSources()
    {
        if (_retiredLoadCts.Count == 0)
        {
            return;
        }

        foreach (var tokenSource in _retiredLoadCts)
        {
            tokenSource.Dispose();
        }

        _retiredLoadCts.Clear();
    }

    public void Dispose()
    {
        _loadLeaguesCts?.Cancel();
        _loadLeaguesCts?.Dispose();
        _loadLeaguesCts = null;

        DisposeRetiredLoadSources();
        _loadLeaguesLock.Dispose();
    }
}
