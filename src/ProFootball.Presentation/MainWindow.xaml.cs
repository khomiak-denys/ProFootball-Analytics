using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private void OnAuthHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private async void OnAuthSignInClick(object sender, RoutedEventArgs e)
    {
        if (FindName("LoginPasswordBox") is not PasswordBox passwordBox)
        {
            return;
        }

        if (!await _viewModel.LoginForm.SignInAsync(passwordBox.Password))
        {
            return;
        }

        await _viewModel.LoadInitialDataAsync();
    }

    private async void OnAuthRegisterClick(object sender, RoutedEventArgs e)
    {
        if (FindName("RegisterPasswordBox") is not PasswordBox passwordBox ||
            FindName("RegisterConfirmPasswordBox") is not PasswordBox confirmPasswordBox)
        {
            return;
        }

        var registerSucceeded = await _viewModel.RegistrationForm.RegisterAsync(
            passwordBox.Password,
            confirmPasswordBox.Password);

        if (!registerSucceeded)
        {
            return;
        }

        _viewModel.LoginForm.Login = _viewModel.RegistrationForm.Login;
        if (FindName("AuthTabs") is TabControl authTabs)
        {
            authTabs.SelectedIndex = 0;
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnToggleMaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnShowLoginModeClick(object sender, RoutedEventArgs e)
    {
        if (FindName("AuthTabs") is TabControl authTabs)
        {
            authTabs.SelectedIndex = 0;
        }
    }

    private void OnShowRegisterModeClick(object sender, RoutedEventArgs e)
    {
        if (FindName("AuthTabs") is TabControl authTabs)
        {
            authTabs.SelectedIndex = 1;
        }
    }

    private void OnSidebarItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox || e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        if (ItemsControl.ContainerFromElement(listBox, source) is ListBoxItem { Tag: AppTab tab })
        {
            _viewModel.SelectedSidebarTab = tab;
            e.Handled = true;
        }
    }
}
