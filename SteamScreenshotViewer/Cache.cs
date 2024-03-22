using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace SteamScreenshotViewer;

public class Cache
{
    public const string cachePath = @"plumbing/cache.json";

    public static Cache Instance => Exists() ? SerializedSingletonRegistry.Load<Cache>() : new Cache();

    public ConcurrentDictionary<string, string> NamesByAppId { get; set; } = new();

    public void StoreAndSerialize()
    {
        SerializedSingletonRegistry.StoreAndSerialize<Cache>(this, true);
    }

    public static bool Exists()
    {
        return Path.Exists(cachePath);
    }
}