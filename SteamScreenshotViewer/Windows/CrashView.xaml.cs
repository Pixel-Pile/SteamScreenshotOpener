using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Serilog;
using SteamScreenshotViewer.Constants;
using SteamScreenshotViewer.Helper;

namespace SteamScreenshotViewer.Views;

public partial class CrashView : Window
{
    private static ILogger log = Log.ForContext<CrashView>();

    public CrashView()
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