using System.Windows;
using ProFootball.Application.Abstractions.Auth;

namespace ProFootball.Presentation;

public partial class MainWindow : Window
{
    private readonly ISessionService _sessionService;

    public MainWindow(ISessionService sessionService)
    {
        _sessionService = sessionService;
        InitializeComponent();
        RenderSessionState();
    }

    private void OnSessionActionClick(object sender, RoutedEventArgs e)
    {
        if (_sessionService.IsAuthenticated)
        {
            _sessionService.SignOut();
        }
        else
        {
            _sessionService.SignIn("local-dev-user");
        }

        RenderSessionState();
    }

    private void RenderSessionState()
    {
        if (_sessionService.IsAuthenticated)
        {
            SessionStateText.Text = $"Authenticated as '{_sessionService.CurrentUser}'.";
            SessionActionButton.Content = "Sign out";
            return;
        }

        SessionStateText.Text = "Anonymous session.";
        SessionActionButton.Content = "Sign in";
    }
}
