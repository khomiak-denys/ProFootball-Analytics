using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Commands;
using ProFootball.Application.Common;

namespace ProFootball.Presentation.ViewModels.Auth;

public sealed class LoginViewModel(ICommandDispatcher commandDispatcher) : ObservableObject
{
    private string _login = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public async Task<bool> SignInAsync(string password, CancellationToken cancellationToken = default)
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Login and password are required.";
            return false;
        }

        IsBusy = true;
        try
        {
            var result = await commandDispatcher.DispatchAsync<SignInCommand, Result>(
                new SignInCommand(Login, password),
                cancellationToken);

            if (result.IsFailure)
            {
                ErrorMessage = result.Error?.Message ?? "Sign in failed.";
                return false;
            }

            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
