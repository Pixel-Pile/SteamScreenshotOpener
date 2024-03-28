using System.Diagnostics;
using System.IO;
using Serilog;

namespace SteamScreenshotViewer.Helper;

public class Config
{
    private static ILogger log = Log.ForContext<Config>();

    public static Config Instance => GetInstance();

    private static Config GetInstance()
    {
        if (SerializedSingletonRegistry.TryGetInstance<Config>(out Config? instance))
        {
            Debug.Assert(instance is not null);
            return instance;
        }

        log.Information("creating new config instance");
        Config config = new();
        SerializedSingletonRegistry.Post(config);
        return config;
    }

    private object lockObject = new();

    private string? _screenshotBasePath;

    public string? ScreenshotBasePath
    {
        get
        {
            lock (lockObject)
            {
                return _screenshotBasePath;
            }
        }
        set
        {
            lock (lockObject)
            {
                _screenshotBasePath = value;
            }
        }
    }

    private bool _isDarkMode;

    public bool IsDarkMode
    {
        get
        {
            Thread.MemoryBarrier();
            return _isDarkMode;
        }
        set
        {
            Thread.MemoryBarrier();
            _isDarkMode = value;
            Thread.MemoryBarrier();
        }
    }

    public void PostAndSerialize()
    {
        SerializedSingletonRegistry.PostAndSerialize<Config>(this, true);
    }
}