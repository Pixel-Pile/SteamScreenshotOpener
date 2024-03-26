using SteamScreenshotViewer.Helper;

namespace SteamScreenshotViewer.Model;

public class ResolvedSteamApp : SteamAppExtension
{
    public ResolvedSteamApp(ISteamApp app, string name) : base(app)
    {
        Name = StringHelper.RemoveDuplicateWhitespace(name);
    }

    public string Name { get; }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}