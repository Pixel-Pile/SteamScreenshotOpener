using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Serilog.Core;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Helper;
using SteamScreenshotViewer.Model;
using SteamScreenshotViewer.Views;
using GameResolver = SteamScreenshotViewer.Helper.GameResolver;

namespace SteamScreenshotViewer;

public enum View
{
    Error,
    Apps,
    BasePathDialog,
    UnresolvedApps,
    Loading
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static ILogger log = Log.ForContext<MainWindow>();

    public MainWindow()
    {
        ConfigureLogger();
        log.Information("program started");
        AppDomain.CurrentDomain.UnhandledException += LogExceptionAndShutdown;
        TaskScheduler.UnobservedTaskException += LogExceptionAndShutdown;
        gameResolver.AutoResolveFinished += HandleAutoResolveFinished;
        gameResolver.AppsFullyResolved += HandleAppsFullyResolved;
        InitializeComponent();
    }

    private static void LogExceptionAndShutdown(Exception e)
    {
        log.Fatal(e, "unhandled exception");
        Log.CloseAndFlush();
        Application.Current.Shutdown();
    }

    private static void LogExceptionAndShutdown(object sender, UnhandledExceptionEventArgs e)
    {
        LogExceptionAndShutdown((Exception)e.ExceptionObject);
    }

    private static void LogExceptionAndShutdown(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogExceptionAndShutdown(e.Exception);
    }

    public static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File($"logs/{FormatDateTimeForFileName(DateTime.Now)}.log")
            .CreateLogger();
    }

    public static string FormatDateTimeForFileName(DateTime time) => $"{time:yyyy-MM-dd_HH-mm-ss}";


    private GameResolver gameResolver = new();

    public static readonly DependencyProperty CurrentViewProperty = DependencyProperty.Register(
        nameof(CurrentView), typeof(TopLevelView), typeof(MainWindow), new PropertyMetadata(default(TopLevelView)));

    public TopLevelView CurrentView
    {
        get { return (TopLevelView)GetValue(CurrentViewProperty); }
        set { SetValue(CurrentViewProperty, value); }
    }

    private void HandleAutoResolveFinished()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Thread.MemoryBarrier();
            if (gameResolver.UnresolvedApps.Count == 0)
            {
                DisplayView(View.Apps);
            }
            else
            {
                DisplayView(View.UnresolvedApps);
            }
        });
    }

    private void HandleAppsFullyResolved()
    {
        Application.Current.Dispatcher.Invoke(() => DisplayView(View.Apps));
    }

    public void DisplayView(View view)
    {
        switch (view)
        {
            case View.Apps:
                LoadAppsView();
                break;
            case View.BasePathDialog:
                LoadBasePathDialogView();
                break;
            case View.UnresolvedApps:
                LoadUnresolvedAppsView();
                break;
            case View.Loading:
                LoadLoadingScreenView();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(view), view,
                    "attempted to load a view that does not exist");
        }
    }

    private void LoadLoadingScreenView()
    {
        ViewLoadingScreen view = new(gameResolver);
        CurrentView = view;
    }

    private void LoadUnresolvedAppsView()
    {
        ViewUnresolvedApps view = new(gameResolver);
        CurrentView = view;
    }

    private void LoadBasePathDialogView()
    {
        ViewBasePathDialog basePathDialog = new(gameResolver);
        basePathDialog.SubmitButtonCommand = new RelayCommand<string>(HandleGameSpecificPathSubmitted);
        CurrentView = basePathDialog;
    }

    private void LoadAppsView()
    {
        ViewApps view = new(gameResolver);
        CurrentView = view;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Start();
    }


    private void HandleGameSpecificPathSubmitted(string gameSpecificScreenshotPath)
    {
        Config config = new Config();
        config.ScreenshotBasePath = ResolveBasePath(gameSpecificScreenshotPath);
        config.PostAndSerialize();
        DisplayView(View.Loading);
        Task.Run(LoadAppList);
    }

    private void Start()
    {
        if (!Config.Exists())
        {
            DisplayView(View.BasePathDialog);
            return;
        }

        DisplayView(View.Loading);
        Task.Run(LoadAppList);
    }

    private async Task LoadAppList()
    {
        await gameResolver.SearchAndResolveApps();
    }


    private string ResolveBasePath(string pathToASpecificGamesScreenshots)
    {
        char[] path = pathToASpecificGamesScreenshots.ToCharArray();
        int separatorsFound = 0;
        int i = path.Length - 1;
        for (; i >= 0; i--)
        {
            if (path[i] == System.IO.Path.DirectorySeparatorChar)
            {
                separatorsFound++;
                if (separatorsFound == 2)
                {
                    break;
                }
            }
        }

        return pathToASpecificGamesScreenshots.Substring(0, i);
    }
}