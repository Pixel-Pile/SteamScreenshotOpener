using System.IO;
using System.Text.Json;

namespace SteamScreenshotViewer;

public class Config
{
    public const string configPath = "plumbing/Config.json";

    public static Config Instance => SerializedSingletonRegistry.Load<Config>();

    public string ScreenshotBasePath { get; set; }

    public void StoreAndSerialize()
    {
        SerializedSingletonRegistry.StoreAndSerialize<Config>(this);
    }

    public static bool Exists()
    {
        return Path.Exists(configPath);
    }
}