using System.Windows;
using System.Windows.Threading;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Templates;
using Serilog.Templates.Themes;
using SteamScreenshotViewer.Constants;
using SteamScreenshotViewer.Helper;
using SteamScreenshotViewer.Views;
using SteamScreenshotViewer.Windows;

namespace SteamScreenshotViewer;

public partial class App : Application
{
    private static ILogger log = Log.ForContext<App>();

    public App()
    {
        ConfigureLogger();
        log.Information("program started");
        AppDomain.CurrentDomain.UnhandledException += HandleAppDomainException;
        Dispatcher.UnhandledException += HandleDispatcherException;
        TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
        TaskHelper.UnobservedTaskException += HandleUnobservedTaskException;
        InitializeComponent();
    }


    private const string TemplateString =
        "{@t:yyyy-MM-dd HH:mm:ss.fff} [{@l:u4}] [{ThreadId}] " +
        "[{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] {@m}\r\n{@x}";

    private static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithThreadId()
            .Enrich.FromLogContext()
            .WriteTo.Console(new ExpressionTemplate(TemplateString,
                theme: TemplateTheme.Code, applyThemeWhenOutputIsRedirected: true))
            .WriteTo.File(new ExpressionTemplate(TemplateString),
                $"{Paths.LogsDir}/{FormatDateTimeForFileName(DateTime.Now)}.log")
            .CreateLogger();
    }

    private static bool openedCrashWindow;

    private static void ShowCrashWindow()
    {
        Current.Dispatcher.Invoke(() =>
        {
            // prevent more than 1 crash window from opening
            if (!openedCrashWindow)
            {
                openedCrashWindow = true;
                new CrashWindow().ShowDialog();
            }
        });
    }

    private static void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // this handler does not appear to be invoked ever? maybe debug side effects
        e.Handled = true;
        log.Fatal(e.Exception, "unhandled exception on dispatcher thread");
        Log.CloseAndFlush();
        ShowCrashWindow();
    }

    private void HandleAppDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        // apparently also triggered on dispatcher exceptions? maybe debug side effects
        log.Fatal(e.ExceptionObject as Exception, "unhandled app domain exception");
        Log.CloseAndFlush();
        ShowCrashWindow();
    }

    public static void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        log.Fatal(e.Exception, "unobserved task exception");
        e.SetObserved();
        Log.CloseAndFlush();
        ShowCrashWindow();
    }


    public static string FormatDateTimeForFileName(DateTime time) => $"{time:yyyy-MM-dd_HH-mm-ss}";
}