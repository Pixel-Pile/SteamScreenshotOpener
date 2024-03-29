using System.Diagnostics;

namespace SteamScreenshotViewer.Core;

public static class ExplorerHelper
{
    public static void OpenExplorerAtPath(string path)
    {
        Process.Start("explorer.exe", path);
    }
}