using System.Collections.ObjectModel;
using ProFootball.Application.Abstractions.Querying;
using ProFootball.Application.Contracts.Analytics;
using ProFootball.Application.Contracts.Queries;
using ProFootball.Presentation.Commands;

namespace ProFootball.Presentation.ViewModels;

public sealed class PlayerDetailsViewModel : ObservableObject
{
    private readonly IPlayersQueryService _playersQueryService;
    private readonly IAnalyticsQueryService _analyticsQueryService;
    private PlayerDetailsDto? _details;
    private int? _selectedPlayerApiId;

    public PlayerDetailsViewModel(
        IPlayersQueryService playersQueryService,
        IAnalyticsQueryService analyticsQueryService)
    {
        _playersQueryService = playersQueryService;
        _analyticsQueryService = analyticsQueryService;
        Attributes = new ObservableCollection<PlayerAttributeDto>();
        TrendPoints = new ObservableCollection<PlayerTrendPointDto>();
        LoadCommand = new AsyncRelayCommand(LoadAsync, () => SelectedPlayerApiId.HasValue);
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

        Details = await _playersQueryService.GetPlayerDetailsAsync(SelectedPlayerApiId.Value);
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

        var trend = await _analyticsQueryService.GetPlayerTrendAsync(SelectedPlayerApiId.Value);
        foreach (var point in trend)
        {
            TrendPoints.Add(point);
        }
    }
}
