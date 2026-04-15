using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class MatchDetailsViewModel : ObservableObject
{
    private readonly IMatchesQueryService _matchesQueryService;
    private MatchDetailsDto? _details;
    private int? _selectedMatchApiId;

    public MatchDetailsViewModel(IMatchesQueryService matchesQueryService)
    {
        _matchesQueryService = matchesQueryService;
        LoadCommand = new AsyncRelayCommand(LoadAsync, CommandExceptionHandler.Handle, () => SelectedMatchApiId.HasValue);
    }

    public int? SelectedMatchApiId
    {
        get => _selectedMatchApiId;
        set
        {
            if (!SetProperty(ref _selectedMatchApiId, value))
            {
                return;
            }

            LoadCommand.RaiseCanExecuteChanged();
        }
    }

    public MatchDetailsDto? Details
    {
        get => _details;
        private set => SetProperty(ref _details, value);
    }

    public AsyncRelayCommand LoadCommand { get; }

    public async Task LoadAsync()
    {
        if (!SelectedMatchApiId.HasValue)
        {
            return;
        }

        Details = await _matchesQueryService.GetMatchDetailsAsync(SelectedMatchApiId.Value);
    }
}
