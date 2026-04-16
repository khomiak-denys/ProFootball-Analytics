using System.Windows;
using Microsoft.Extensions.Logging;
using ProFootball.Presentation.ViewModels;

namespace ProFootball.Presentation;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel, ILogger<MainWindow> logger)
    {
        _viewModel = viewModel;
        _logger = logger;
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        try
        {
            await _viewModel.LoadInitialDataAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Initial ProFootball data loading failed.");
            MessageBox.Show(
                this,
                "Failed to load initial data. Check database connection and dataset import state.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
