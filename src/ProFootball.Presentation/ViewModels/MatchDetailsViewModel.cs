using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Match.Dtos;
using ProFootball.Application.Match.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class MatchDetailsViewModel : ObservableObject
{
    private readonly IQueryDispatcher _queryDispatcher;
    private MatchDetailsDto? _details;
    private int? _selectedMatchApiId;

    public MatchDetailsViewModel(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
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

        Details = await _queryDispatcher.DispatchAsync<GetMatchDetailsQuery, MatchDetailsDto?>(
            new GetMatchDetailsQuery(SelectedMatchApiId.Value));
    }
}
