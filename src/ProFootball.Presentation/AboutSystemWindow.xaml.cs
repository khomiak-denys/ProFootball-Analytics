using System.Windows;

namespace ProFootball.Presentation;

public partial class AboutSystemWindow : Window
{
    public AboutSystemWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
