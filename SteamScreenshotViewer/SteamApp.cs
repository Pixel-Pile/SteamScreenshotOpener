namespace SteamScreenshotViewer;

public class SteamApp
{
    public string Id {get;}
    public string Name {get;}
    public string ScreenshotsPath {get;}

    public SteamApp(string id, string name, string screenshotsPath)
    {
        Id = id;
        Name = name;
        ScreenshotsPath = screenshotsPath;
    }
}