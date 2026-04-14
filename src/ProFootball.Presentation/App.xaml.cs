using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProFootball.Infrastructure;
using ProFootball.Presentation.ViewModels;

namespace ProFootball.Presentation;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private IServiceScope? _uiScope;

    protected override void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.SetBasePath(AppContext.BaseDirectory);
                configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                configurationBuilder.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddInfrastructure(context.Configuration);
                services.AddScoped<MainViewModel>();
                services.AddScoped<MainWindow>();
            })
            .Build();

        _host.Start();

        _uiScope = _host.Services.CreateScope();
        var mainWindow = _uiScope.ServiceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _uiScope?.Dispose();
        _uiScope = null;

        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
            _host = null;
        }

        base.OnExit(e);
    }
}
