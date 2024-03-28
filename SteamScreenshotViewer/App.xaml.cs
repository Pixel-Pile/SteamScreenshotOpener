using System.Configuration;
using System.Data;
using System.Windows;
using Serilog;

namespace SteamScreenshotViewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static ILogger log = Log.ForContext<App>();

    public App()
    {
        ConfigureLogger();
        log.Information("program started");
        AppDomain.CurrentDomain.UnhandledException += LogExceptionAndShutdown;
        TaskScheduler.UnobservedTaskException += LogExceptionAndShutdown;
        InitializeComponent();
    }

    private static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File($"logs/{FormatDateTimeForFileName(DateTime.Now)}.log")
            .CreateLogger();
    }

    private static void LogExceptionAndShutdown(Exception e)
    {
        log.Fatal(e, "unhandled exception");
        Log.CloseAndFlush();
        Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
    }

    private static void LogExceptionAndShutdown(object sender, UnhandledExceptionEventArgs e)
    {
        LogExceptionAndShutdown((Exception)e.ExceptionObject);
    }

    private static void LogExceptionAndShutdown(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogExceptionAndShutdown(e.Exception);
    }


    public static string FormatDateTimeForFileName(DateTime time) => $"{time:yyyy-MM-dd_HH-mm-ss}";
}