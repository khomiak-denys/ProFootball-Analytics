using ProFootball.Application.Abstractions.Cqrs;
using ProFootball.Application.Auth.Commands;

namespace ProFootball.Presentation.ViewModels.Auth;

public sealed class RegistrationViewModel(ICommandDispatcher commandDispatcher) : ObservableObject
{
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _login = string.Empty;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _isBusy;

    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

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

    public string SuccessMessage
    {
        get => _successMessage;
        private set => SetProperty(ref _successMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public async Task<bool> RegisterAsync(string password, string confirmPassword, CancellationToken cancellationToken = default)
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Login))
        {
            ErrorMessage = "First name, last name and login are required.";
            return false;
        }

        IsBusy = true;
        try
        {
            await commandDispatcher.DispatchAsync(
                new RegisterUserCommand(FirstName, LastName, Login, password, confirmPassword),
                cancellationToken);

            SuccessMessage = "Registration completed. You can now sign in.";
            return true;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
