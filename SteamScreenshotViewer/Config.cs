using System.IO;
using System.Text.Json;

namespace SteamScreenshotViewer;

public class Config
{
    public const string configPath = "plumbing/Config.json";

    public static Config Instance => GetInstance();

    private static Config GetInstance()
    {
        if (SerializedSingletonRegistry.TryGetInstance<Config>(out Config? instance))
        {
            return instance;
        }

        throw new InvalidOperationException("no config instance has yet been posted");
    }


    public string ScreenshotBasePath { get; set; }

    public void PostAndSerialize()
    {
        SerializedSingletonRegistry.PostAndSerialize<Config>(this);
    }

    public static bool Exists()
    {
        return Path.Exists(configPath);
    }
}