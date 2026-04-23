using System.Windows;
using ProFootball.Presentation.ViewModels.Auth;

namespace ProFootball.Presentation.Views.Auth;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private readonly RegistrationWindow _registrationWindow;

    public LoginWindow(LoginViewModel viewModel, RegistrationWindow registrationWindow)
    {
        _viewModel = viewModel;
        _registrationWindow = registrationWindow;
        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void OnSignInClick(object sender, RoutedEventArgs e)
    {
        var loginSucceeded = await _viewModel.SignInAsync(PasswordInput.Password);
        if (!loginSucceeded)
        {
            return;
        }

        DialogResult = true;
        Close();
    }

    private void OnOpenRegistrationClick(object sender, RoutedEventArgs e)
    {
        _registrationWindow.Owner = this;
        var registrationResult = _registrationWindow.ShowDialog();
        if (registrationResult == true)
        {
            _viewModel.Login = _registrationWindow.RegisteredLogin;
            PasswordInput.Password = string.Empty;
            Activate();
        }
    }
}
