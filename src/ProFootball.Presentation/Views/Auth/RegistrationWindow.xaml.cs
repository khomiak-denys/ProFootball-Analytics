using System.Windows;
using ProFootball.Presentation.ViewModels.Auth;

namespace ProFootball.Presentation.Views.Auth;

public partial class RegistrationWindow : Window
{
    private readonly RegistrationViewModel _viewModel;

    public RegistrationWindow(RegistrationViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
    }

    public string RegisteredLogin { get; private set; } = string.Empty;

    private async void OnRegisterClick(object sender, RoutedEventArgs e)
    {
        var registerSucceeded = await _viewModel.RegisterAsync(
            PasswordInput.Password,
            ConfirmPasswordInput.Password);

        if (!registerSucceeded)
        {
            return;
        }

        RegisteredLogin = _viewModel.Login;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
