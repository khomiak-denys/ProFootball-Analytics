using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class MatchesViewModel : ObservableObject
{
    private readonly IMatchesQueryService _matchesQueryService;
    private readonly ICountriesLeaguesQueryService _countriesLeaguesQueryService;
    private readonly Action<int> _openDetails;
    private MatchListItemDto? _selectedMatch;
    private LeagueDto? _selectedLeague;
    private string? _season;
    private int? _teamApiId;
    private int _page = 1;
    private int _pageSize = 25;
    private int _totalCount;

    public MatchesViewModel(
        IMatchesQueryService matchesQueryService,
        ICountriesLeaguesQueryService countriesLeaguesQueryService,
        Action<int> openDetails)
    {
        _matchesQueryService = matchesQueryService;
        _countriesLeaguesQueryService = countriesLeaguesQueryService;
        _openDetails = openDetails;

        Matches = new ObservableCollection<MatchListItemDto>();
        Leagues = new ObservableCollection<LeagueDto>();
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        LoadLeaguesCommand = new AsyncRelayCommand(LoadLeaguesAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => HasPreviousPage);
        OpenDetailsCommand = new RelayCommand(OpenDetails, () => SelectedMatch is not null);
    }

    public ObservableCollection<MatchListItemDto> Matches { get; }

    public ObservableCollection<LeagueDto> Leagues { get; }

    public MatchListItemDto? SelectedMatch
    {
        get => _selectedMatch;
        set
        {
            if (!SetProperty(ref _selectedMatch, value))
            {
                return;
            }

            OpenDetailsCommand.RaiseCanExecuteChanged();
        }
    }

    public LeagueDto? SelectedLeague
    {
        get => _selectedLeague;
        set => SetProperty(ref _selectedLeague, value);
    }

    public string? Season
    {
        get => _season;
        set => SetProperty(ref _season, value);
    }

    public int? TeamApiId
    {
        get => _teamApiId;
        set => SetProperty(ref _teamApiId, value);
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
        set => SetProperty(ref _pageSize, value);
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

    public AsyncRelayCommand SearchCommand { get; }

    public AsyncRelayCommand LoadLeaguesCommand { get; }

    public AsyncRelayCommand NextPageCommand { get; }

    public AsyncRelayCommand PreviousPageCommand { get; }

    public RelayCommand OpenDetailsCommand { get; }

    public async Task LoadLeaguesAsync()
    {
        Leagues.Clear();
        var leagues = await _countriesLeaguesQueryService.GetLeaguesAsync();
        foreach (var league in leagues)
        {
            Leagues.Add(league);
        }
    }

    public async Task SearchAsync()
    {
        var result = await _matchesQueryService.SearchMatchesAsync(new MatchSearchQuery(
            SelectedLeague?.Id,
            string.IsNullOrWhiteSpace(Season) ? null : Season.Trim(),
            TeamApiId,
            null,
            null,
            "date",
            true,
            Page,
            PageSize));

        TotalCount = result.TotalCount;
        Matches.Clear();
        foreach (var item in result.Items)
        {
            Matches.Add(item);
        }

        NextPageCommand.RaiseCanExecuteChanged();
        PreviousPageCommand.RaiseCanExecuteChanged();
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

    private void OpenDetails()
    {
        if (SelectedMatch is null)
        {
            return;
        }

        _openDetails(SelectedMatch.MatchApiId);
    }
}
