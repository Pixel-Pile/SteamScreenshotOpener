using System.Diagnostics;
using System.Windows.Navigation;

namespace SteamScreenshotViewer.Helper;

public static class HyperlinkHelper
{
    public static void OpenHyperlink(RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
    }
}