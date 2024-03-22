namespace SteamScreenshotViewer.Model;

public interface ISteamApp
{
    public string Id { get; }
    public string ScreenshotsPath { get; }
}