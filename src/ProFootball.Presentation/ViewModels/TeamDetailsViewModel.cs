using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Team.Dtos;
using ProFootball.Application.Team.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class TeamDetailsViewModel : ObservableObject
{
    private readonly IQueryDispatcher _queryDispatcher;
    private TeamDetailsDto? _details;
    private int? _selectedTeamApiId;

    public TeamDetailsViewModel(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
        Attributes = new ObservableCollection<TeamAttributeDto>();
        LoadCommand = new AsyncRelayCommand(LoadAsync, CommandExceptionHandler.Handle, () => SelectedTeamApiId.HasValue);
    }

    public int? SelectedTeamApiId
    {
        get => _selectedTeamApiId;
        set
        {
            if (!SetProperty(ref _selectedTeamApiId, value))
            {
                return;
            }

            LoadCommand.RaiseCanExecuteChanged();
        }
    }

    public TeamDetailsDto? Details
    {
        get => _details;
        private set => SetProperty(ref _details, value);
    }

    public ObservableCollection<TeamAttributeDto> Attributes { get; }

    public AsyncRelayCommand LoadCommand { get; }

    public async Task LoadAsync()
    {
        if (!SelectedTeamApiId.HasValue)
        {
            return;
        }

        Details = await _queryDispatcher.DispatchAsync<GetTeamDetailsQuery, TeamDetailsDto?>(
            new GetTeamDetailsQuery(SelectedTeamApiId.Value));
        Attributes.Clear();
        if (Details is null)
        {
            return;
        }

        foreach (var attribute in Details.Attributes)
        {
            Attributes.Add(attribute);
        }
    }
}
