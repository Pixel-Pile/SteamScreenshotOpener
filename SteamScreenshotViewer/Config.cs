using System.IO;
using System.Text.Json;
using Serilog;

namespace SteamScreenshotViewer;

public class Config
{
    private static ILogger log = Log.ForContext<Config>();

    public const string configPath = "storage/Config.json";

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