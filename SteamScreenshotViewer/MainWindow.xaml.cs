using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Model;
using SteamScreenshotViewer.Views;

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
    public MainWindow()
    {
        TaskScheduler.UnobservedTaskException += Rethrow;
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
        ViewLoadingScreen view = new();
        view.GameResolver = gameResolver;
        CurrentView = view;
    }

    private void LoadUnresolvedAppsView()
    {
        ViewUnresolvedApps view = new();
        view.GameResolver = gameResolver;
        CurrentView = view;
    }

    private void LoadBasePathDialogView()
    {
        ViewBasePathDialog basePathDialog = new();
        basePathDialog.SubmitButtonCommand = new RelayCommand<string>(HandleGameSpecificPathSubmitted);
        CurrentView = basePathDialog;
    }

    private void LoadAppsView()
    {
        ViewApps view = new();
        view.GameResolver = gameResolver;
        CurrentView = view;
    }


    private void Rethrow(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Console.WriteLine("unobserved exception");
        throw e.Exception;
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