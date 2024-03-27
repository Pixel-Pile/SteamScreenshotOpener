using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
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
[INotifyPropertyChanged]
public partial class MainWindow : Window
{
    private static ILogger log = Log.ForContext<MainWindow>();

    public MainWindow()
    {
        gameResolver.AutoResolveFinished += HandleAutoResolveFinished;
        gameResolver.AppsFullyResolved += HandleAppsFullyResolved;
        InitializeComponent();
    }

    private GameResolver gameResolver = new();

    public static readonly DependencyProperty CurrentViewProperty = DependencyProperty.Register(
        nameof(CurrentView), typeof(TopLevelView), typeof(MainWindow), new PropertyMetadata(default(TopLevelView)));

    public TopLevelView CurrentView
    {
        get { return (TopLevelView)GetValue(CurrentViewProperty); }
        set { SetValue(CurrentViewProperty, value); }
    }

    [ObservableProperty] private bool isDarkMode;

    private void HandleAutoResolveFinished()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Thread.MemoryBarrier();
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
        log.Information("loading view " + view);
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

    private void OnThemeIconClick(object sender, MouseButtonEventArgs e)
    {
        var paletteHelper = new PaletteHelper();
        Theme theme = paletteHelper.GetTheme();
        if (IsDarkMode)
        {
            theme.SetDarkTheme();
        }
        else
        {
            theme.SetLightTheme();
        }

        paletteHelper.SetTheme(theme);
        IsDarkMode = !IsDarkMode;
    }
}