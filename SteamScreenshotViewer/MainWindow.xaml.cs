using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace SteamScreenshotViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private ICollection<SteamApp> _steamApps;

    public ICollection<SteamApp> SteamApps
    {
        get => _steamApps;
        set
        {
            _steamApps = value;
            NotifyPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    public MainWindow()
    {
        TaskScheduler.UnobservedTaskException += Rethrow;
        InitializeComponent();
        SubmitBaseUrlCommand = new(HandleBasePathSubmitted);
        DataContext = this;
    }

    private void Rethrow(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Console.WriteLine("unobserved exception");
        throw e.Exception;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        StartIfBasePathKnown();
    }

    public static readonly DependencyProperty BasePathProperty = DependencyProperty.Register(
        nameof(BasePath), typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

    public string BasePath
    {
        get { return (string)GetValue(BasePathProperty); }
        set { SetValue(BasePathProperty, value); }
    }

    public RelayCommand SubmitBaseUrlCommand { get; set; }

    private void HandleBasePathSubmitted()
    {
        Config config = Config.CreateEmpty();

        string gameSpecificScreenshotPath = BasePath;
        if (string.IsNullOrEmpty(gameSpecificScreenshotPath))
        {
            MessageBox.Show("base path cannot be empty");
            return;
        }

        if (!System.IO.Path.Exists(gameSpecificScreenshotPath))
        {
            MessageBox.Show("path does not exist: " + gameSpecificScreenshotPath);
            return;
        }

        PanelEnterBasePath.Visibility = Visibility.Collapsed;
        config.ScreenshotBasePath = ResolveBasePath(gameSpecificScreenshotPath);
        config.Commit();
        Task.Run(LoadAppList);
    }

    private void StartIfBasePathKnown()
    {
        if (!Config.Exists())
        {
            AskForBasePath();
            return;
        }

        Task.Run(LoadAppList);
    }

    private async Task LoadAppList()
    {
        GameResolver resolver = new GameResolver();
        SteamApp[] apps = await resolver.FindGameDirectories();
        Array.Sort(apps, (app1, app2) => app1.Name.CompareTo(app2.Name));
        SteamApps = apps;
    }

    private void AskForBasePath()
    {
        PanelEnterBasePath.Visibility = Visibility.Visible;
    }

    private string ResolveBasePath(string pathToASpecificGamesScreenshots)
    {
        if (!pathToASpecificGamesScreenshots.EndsWith("screenshots"))
        {
            throw new ArgumentException($"path '{pathToASpecificGamesScreenshots}' not recognized as screenshot path");
        }

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

    private void OnAppClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as ListViewItem)?.Content is SteamApp steamApp)
        {
            Process.Start("explorer.exe", steamApp.ScreenshotsPath);
        }
    }
}