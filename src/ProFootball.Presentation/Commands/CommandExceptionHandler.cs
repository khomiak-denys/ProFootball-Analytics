using System.Diagnostics;
using System.Windows;

namespace ProFootball.Presentation.Commands;

public static class CommandExceptionHandler
{
    public static void Handle(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return;
        }

        Trace.TraceError(exception.ToString());

        try
        {
            if (System.Windows.Application.Current is null)
            {
                return;
            }

            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                MessageBox.Show(
                    "Operation failed. Check logs for details.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "Operation failed. Check logs for details.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        }
        catch
        {
        }
    }
}
