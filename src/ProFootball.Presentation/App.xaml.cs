using System.Windows;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProFootball.Infrastructure;
using ProFootball.Presentation.Services;
using ProFootball.Presentation.ViewModels;
using Serilog;
using Serilog.Events;

namespace ProFootball.Presentation;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private IServiceScope? _uiScope;

    protected override void OnStartup(StartupEventArgs e)
    {
        var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);
        var logFilePath = Path.Combine(logsDirectory, "profootball-.log");
#if DEBUG
        const LogEventLevel minimumLogLevel = LogEventLevel.Debug;
#else
        const LogEventLevel minimumLogLevel = LogEventLevel.Warning;
#endif
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLogLevel)
            .Enrich.FromLogContext()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.SetBasePath(AppContext.BaseDirectory);
                configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                configurationBuilder.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddInfrastructure(context.Configuration);
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddScoped<MainViewModel>();
                services.AddScoped<MainWindow>();
            })
            .Build();

        _host.Start();

        _uiScope = _host.Services.CreateScope();
        var themeService = _uiScope.ServiceProvider.GetRequiredService<IThemeService>();
        themeService.Initialize();
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

        Log.CloseAndFlush();

        base.OnExit(e);
    }
}
