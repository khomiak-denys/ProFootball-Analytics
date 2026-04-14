using System.Windows.Input;

namespace ProFootball.Presentation.Commands;

public sealed class AsyncRelayCommand(
    Func<Task> executeAsync,
    Func<bool>? canExecute = null,
    Action<Exception>? onException = null) : ICommand
{
    private bool _isRunning;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isRunning && (canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isRunning = true;
        RaiseCanExecuteChanged();
        try
        {
            await executeAsync();
        }
        catch (Exception exception)
        {
            if (onException is not null)
            {
                onException(exception);
            }
            else
            {
                System.Diagnostics.Trace.TraceError(exception.ToString());
            }
        }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
