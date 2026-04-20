using System.Windows;
using System.Windows.Threading;
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
    private string? _logFilePath;

    protected override void OnStartup(StartupEventArgs e)
    {
        var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);
        _logFilePath = Path.Combine(logsDirectory, "profootball-.log");
#if DEBUG
        const LogEventLevel minimumLogLevel = LogEventLevel.Debug;
#else
        const LogEventLevel minimumLogLevel = LogEventLevel.Warning;
#endif
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLogLevel)
            .Enrich.FromLogContext()
            .WriteTo.File(
                _logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

        try
        {
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
        catch (Exception exception)
        {
            ReportFatalException("Application startup failed.", exception);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnTaskSchedulerUnobservedTaskException;

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

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportFatalException("An unhandled UI exception occurred.", e.Exception);
        e.Handled = true;
        Shutdown(-1);
    }

    private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            ReportFatalException("A critical runtime exception occurred.", exception);
            return;
        }

        ReportFatalMessage("A critical runtime exception occurred without exception details.");
    }

    private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ReportFatalException("An unobserved background task exception occurred.", e.Exception);
        e.SetObserved();
    }

    private void ReportFatalException(string title, Exception exception)
    {
        try
        {
            Log.Fatal(exception, "{Title}", title);
        }
        catch
        {
            // Ignore logging failures and still show user-facing diagnostics.
        }

        ShowFatalMessageBox($"{title}{Environment.NewLine}{Environment.NewLine}{exception.Message}");
    }

    private static void ReportFatalMessage(string message)
    {
        ShowFatalMessageBox(message);
    }

    private static void ShowFatalMessageBox(string details)
    {
        MessageBox.Show(
            details,
            "ProFootball startup error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
