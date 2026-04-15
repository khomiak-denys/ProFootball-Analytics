using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class TeamsViewModel : ObservableObject
{
    private readonly ITeamsQueryService _teamsQueryService;
    private readonly Action<int> _openDetails;
    private TeamListItemDto? _selectedTeam;
    private string? _nameFilter;
    private int _page = 1;
    private int _pageSize = 25;
    private int _totalCount;

    public TeamsViewModel(ITeamsQueryService teamsQueryService, Action<int> openDetails)
    {
        _teamsQueryService = teamsQueryService;
        _openDetails = openDetails;

        Teams = new ObservableCollection<TeamListItemDto>();
        SearchCommand = new AsyncRelayCommand(StartSearchAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => HasPreviousPage);
        OpenDetailsCommand = new RelayCommand(OpenDetails, () => SelectedTeam is not null);
    }

    public ObservableCollection<TeamListItemDto> Teams { get; }

    public TeamListItemDto? SelectedTeam
    {
        get => _selectedTeam;
        set
        {
            if (!SetProperty(ref _selectedTeam, value))
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
        var result = await _teamsQueryService.SearchTeamsAsync(new TeamSearchQuery(
            NameFilter,
            "LongName",
            false,
            Page,
            PageSize));

        TotalCount = result.TotalCount;
        Teams.Clear();
        foreach (var item in result.Items)
        {
            Teams.Add(item);
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
        if (SelectedTeam is null)
        {
            return;
        }

        _openDetails(SelectedTeam.TeamApiId);
    }
}
