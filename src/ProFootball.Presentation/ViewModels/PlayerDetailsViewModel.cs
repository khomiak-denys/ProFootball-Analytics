using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Player.Dtos;
using ProFootball.Application.Player.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class PlayerDetailsViewModel : ObservableObject
{
    private readonly IQueryDispatcher _queryDispatcher;
    private PlayerDetailsDto? _details;
    private int? _selectedPlayerApiId;

    public PlayerDetailsViewModel(
        IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
        Attributes = new ObservableCollection<PlayerAttributeDto>();
        TrendPoints = new ObservableCollection<PlayerTrendPointDto>();
        LoadCommand = new AsyncRelayCommand(LoadAsync, CommandExceptionHandler.Handle, () => SelectedPlayerApiId.HasValue);
    }

    public int? SelectedPlayerApiId
    {
        get => _selectedPlayerApiId;
        set
        {
            if (!SetProperty(ref _selectedPlayerApiId, value))
            {
                return;
            }

            LoadCommand.RaiseCanExecuteChanged();
        }
    }

    public PlayerDetailsDto? Details
    {
        get => _details;
        private set => SetProperty(ref _details, value);
    }

    public ObservableCollection<PlayerAttributeDto> Attributes { get; }

    public ObservableCollection<PlayerTrendPointDto> TrendPoints { get; }

    public AsyncRelayCommand LoadCommand { get; }

    public async Task LoadAsync()
    {
        if (!SelectedPlayerApiId.HasValue)
        {
            return;
        }

        Details = await _queryDispatcher.DispatchAsync<GetPlayerDetailsQuery, PlayerDetailsDto?>(
            new GetPlayerDetailsQuery(SelectedPlayerApiId.Value));
        Attributes.Clear();
        TrendPoints.Clear();

        if (Details is null)
        {
            return;
        }

        foreach (var attribute in Details.Attributes)
        {
            Attributes.Add(attribute);
        }

        var trend = await _queryDispatcher.DispatchAsync<GetPlayerTrendQuery, IReadOnlyList<PlayerTrendPointDto>>(
            new GetPlayerTrendQuery(SelectedPlayerApiId.Value));
        foreach (var point in trend)
        {
            TrendPoints.Add(point);
        }
    }
}
