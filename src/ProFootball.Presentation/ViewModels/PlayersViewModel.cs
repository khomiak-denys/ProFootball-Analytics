using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class PlayersViewModel : ObservableObject
{
    private readonly IPlayersQueryService _playersQueryService;
    private readonly Action<int> _openDetails;
    private PlayerListItemDto? _selectedPlayer;
    private string? _nameFilter;
    private string? _preferredFoot;
    private int? _minOverall;
    private int? _minPotential;
    private int _page = 1;
    private int _pageSize = 25;
    private int _totalCount;

    public PlayersViewModel(IPlayersQueryService playersQueryService, Action<int> openDetails)
    {
        _playersQueryService = playersQueryService;
        _openDetails = openDetails;

        Players = new ObservableCollection<PlayerListItemDto>();
        SearchCommand = new AsyncRelayCommand(StartSearchAsync, CommandExceptionHandler.Handle);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, CommandExceptionHandler.Handle, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, CommandExceptionHandler.Handle, () => HasPreviousPage);
        OpenDetailsCommand = new RelayCommand(OpenDetails, () => SelectedPlayer is not null);
    }

    public ObservableCollection<PlayerListItemDto> Players { get; }

    public PlayerListItemDto? SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            if (!SetProperty(ref _selectedPlayer, value))
            {
                return;
            }

            OpenDetailsCommand.RaiseCanExecuteChanged();
        }
    }

    public string? NameFilter
    {
        get => _nameFilter;
        set => SetProperty(ref _nameFilter, value);
    }

    public string? PreferredFoot
    {
        get => _preferredFoot;
        set => SetProperty(ref _preferredFoot, value);
    }

    public int? MinOverall
    {
        get => _minOverall;
        set => SetProperty(ref _minOverall, value);
    }

    public int? MinPotential
    {
        get => _minPotential;
        set => SetProperty(ref _minPotential, value);
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

    public AsyncRelayCommand NextPageCommand { get; }

    public AsyncRelayCommand PreviousPageCommand { get; }

    public RelayCommand OpenDetailsCommand { get; }

    private async Task StartSearchAsync()
    {
        Page = 1;
        await SearchAsync();
    }

    public async Task SearchAsync()
    {
        var result = await _playersQueryService.SearchPlayersAsync(new PlayerSearchQuery(
            NameFilter,
            MinOverall,
            null,
            MinPotential,
            null,
            null,
            null,
            PreferredFoot,
            "name",
            false,
            Page,
            PageSize));

        TotalCount = result.TotalCount;
        Players.Clear();
        foreach (var item in result.Items)
        {
            Players.Add(item);
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
        if (SelectedPlayer is null)
        {
            return;
        }

        _openDetails(SelectedPlayer.PlayerApiId);
    }
}
