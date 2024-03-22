using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer;

public class SteamApp : ISteamApp
{
    public string Id { get; }
    public string ScreenshotsPath { get; }

    public SteamApp(string id, string screenshotsPath)
    {
        Id = id;
        ScreenshotsPath = screenshotsPath;
    }
}