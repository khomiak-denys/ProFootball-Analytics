using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class CountriesLeaguesViewModel : ObservableObject
{
    private readonly ICountriesLeaguesQueryService _service;
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

            _ = LoadLeaguesAsync();
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

        await LoadLeaguesAsync();
    }

    private async Task LoadLeaguesAsync()
    {
        Leagues.Clear();
        var leagues = await _service.GetLeaguesAsync(SelectedCountry?.Id);
        foreach (var league in leagues)
        {
            Leagues.Add(league);
        }
    }
}
