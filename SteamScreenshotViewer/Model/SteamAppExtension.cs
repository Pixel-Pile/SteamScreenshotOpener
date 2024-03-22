namespace SteamScreenshotViewer.Model;

public class SteamAppExtension : ISteamApp
{
    private readonly ISteamApp app;

    public SteamAppExtension(ISteamApp app)
    {
        this.app = app;
    }

    public string Id => app.Id;

    public string ScreenshotsPath => app.ScreenshotsPath;
}