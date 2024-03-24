namespace SteamScreenshotViewer.Model;

public enum ResolveMeans
{
    Error,
    SteamApi,
    Manual
}

public class ResolvedSteamApp : SteamAppExtension
{
    public ResolvedSteamApp(ISteamApp app, string name) : base(app)
    {
        Name = name;
        LowerCaseName = name.ToLower();
    }

    public ResolveMeans ResolveMeans { get; set; }

    public string Name { get; }
    public string LowerCaseName { get;}

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}