using System.Diagnostics;

namespace SteamScreenshotViewer.Helper;

public static class ExplorerHelper
{
    public static void OpenExplorerAtPath(string path)
    {
        Process.Start("explorer.exe", path);
    }
}