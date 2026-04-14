using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class CountriesLeaguesViewModel : ObservableObject
{
    private readonly ICountriesLeaguesQueryService _service;
    private readonly SemaphoreSlim _loadLeaguesLock = new(1, 1);
    private CancellationTokenSource? _loadLeaguesCts;
    private CountryDto? _selectedCountry;

    public CountriesLeaguesViewModel(ICountriesLeaguesQueryService service)
    {
        _service = service;
        Countries = new ObservableCollection<CountryDto>();
        Leagues = new ObservableCollection<LeagueDto>();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
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

    public async Task RefreshAsync()
    {
        Countries.Clear();
        var countries = await _service.GetCountriesAsync();
        foreach (var country in countries)
        {
            Countries.Add(country);
        }

        await ReloadLeaguesSafeAsync();
    }

    private async Task ReloadLeaguesSafeAsync()
    {
        var loadToken = ReplaceLoadToken();
        var lockAcquired = false;

        await _loadLeaguesLock.WaitAsync(loadToken);
        lockAcquired = true;
        try
        {
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
        catch (Exception)
        {
        }
        finally
        {
            if (lockAcquired)
            {
                _loadLeaguesLock.Release();
            }
        }
    }

    private CancellationToken ReplaceLoadToken()
    {
        _loadLeaguesCts?.Cancel();
        _loadLeaguesCts?.Dispose();
        _loadLeaguesCts = new CancellationTokenSource();
        return _loadLeaguesCts.Token;
    }
}
