using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProFootball.Application.Abstractions.Auth;
using ProFootball.Application.Services.Auth;
using ProFootball.Infrastructure;

namespace ProFootball.Presentation;

public partial class App : System.Windows.Application
{
    private IHost? _host;

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
                services.AddSingleton<ISessionService, InMemorySessionService>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        _host.Start();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
