using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Navigation;
using Serilog;
using SteamScreenshotViewer.Constants;
using SteamScreenshotViewer.Core;
using SteamScreenshotViewer.Helper;

namespace SteamScreenshotViewer.Windows;

public partial class CrashWindow : Window
{
    private static ILogger log = Log.ForContext<CrashWindow>();

    public CrashWindow()
    {
        InitializeComponent();
        this.Closing += CrashView_OnClosing;
    }

    private void OpenHyperlink(object sender, RequestNavigateEventArgs e)
    {
        HyperlinkHelper.OpenHyperlink(e);
    }


    private void OpenLogsDirectory(object sender, RequestNavigateEventArgs e)
    {
        string workingDir = Directory.GetCurrentDirectory();
        string logsDir = workingDir + Path.DirectorySeparatorChar + Paths.LogsDir;
        if (Path.Exists(logsDir))
        {
            ExplorerHelper.OpenExplorerAtPath(logsDir);
        }
        // else: fail silently
        // no logging possible here anymore (Logger.CloseAndFlush already called)
    }

    private void CrashView_OnClosing(object? sender, CancelEventArgs e)
    {
        Dispatcher.Invoke(() => Application.Current.Shutdown());
    }
}