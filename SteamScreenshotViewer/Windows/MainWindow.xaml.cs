using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Serilog;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Core;
using SteamScreenshotViewer.Helper;
using SteamScreenshotViewer.Views;

namespace SteamScreenshotViewer.Windows;

[INotifyPropertyChanged]
public partial class MainWindow : Window
{
    private static ILogger log = Log.ForContext<MainWindow>();

    private Conductor conductor = new();

    public MainWindow()
    {
        conductor.PromptForScreenshotPath += HandlePromptForScreenshotPath;
        conductor.AutoResolveStarted += HandleAutoResolveStarted;
        conductor.AutoResolveFinishedPartialSuccess += HandleAutoResolveFinishedPartialSuccess;
        conductor.AutoResolveFinishedFullSuccess += HandleAutoResolveFinishedFullSuccess;
        conductor.AutoResolveFailed += HandleAutoResolveFailed;
        LoadThemeSpecifiedByConfig();
        InitializeComponent();
        conductor.Start();
    }

    [ObservableProperty] private TopLevelView currentView;
    [ObservableProperty] private bool isDarkMode;
    private Action<string> basePathSetCallback;

    private void HandlePromptForScreenshotPath(object sender, PromptForScreenshotPathEventArgs e)
    {
        this.basePathSetCallback = e.SetScreenshotPathCallback;
        DisplayView(View.BasePathDialog);
    }

    private void HandleAutoResolveStarted()
    {
        DisplayView(View.Loading);
    }

    private void HandleAutoResolveFinishedPartialSuccess()
    {
        DisplayView(View.UnresolvedApps);
    }

    private void HandleAutoResolveFinishedFullSuccess()
    {
        DisplayView(View.Apps);
    }

    private void HandleAutoResolveFailed()
    {
        DisplayView(View.NetworkFailure);
    }

    private void DisplayView(View view)
    {
        log.Information("enqueing view loading for " + view);
        Application.Current.Dispatcher.BeginInvoke(() => DisplayViewOnSameThread(view));
    }

    private void DisplayViewOnSameThread(View view)
    {
        log.Information("loading view " + view);
        switch (view)
        {
            case View.Apps:
                CurrentView = new ViewApps(conductor);
                break;
            case View.BasePathDialog:
                LoadBasePathDialogView();
                break;
            case View.Loading:
                CurrentView = new ViewLoadingScreen(conductor);
                break;
            case View.NetworkFailure:
                CurrentView = new ViewNetworkFailure(conductor);
                break;
            case View.UnresolvedApps:
                CurrentView = new ViewUnresolvedApps(conductor);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(view), view,
                    "attempted to load a view that does not exist");
        }
    }

    private void LoadBasePathDialogView()
    {
        ViewBasePathDialog basePathDialog = new()
        {
            SubmitButtonCommand = new RelayCommand<string>(basePathSetCallback!)
        };
        CurrentView = basePathDialog;
    }

    private static void LoadTheme(bool isDarkMode)
    {
        PaletteHelper paletteHelper = new PaletteHelper();
        Theme theme = paletteHelper.GetTheme();
        if (isDarkMode)
        {
            theme.SetDarkTheme();
        }
        else
        {
            theme.SetLightTheme();
        }

        paletteHelper.SetTheme(theme);
    }

    private void LoadThemeSpecifiedByConfig()
    {
        Config config = Config.Instance;
        IsDarkMode = config.IsDarkMode;
        LoadTheme(config.IsDarkMode);
    }

    private void OnThemeIconClick(object sender, MouseButtonEventArgs e)
    {
        IsDarkMode = !IsDarkMode;
        LoadTheme(IsDarkMode);
        Config config = Config.Instance;
        config.IsDarkMode = IsDarkMode;
        config.PostAndSerialize();
    }
}