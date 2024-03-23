using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SteamScreenshotViewer;

public class Cache
{
    public const string cachePath = @"plumbing/cache.json";

    public static Cache Instance => GetInstance();

    public ConcurrentDictionary<string, string> NamesByAppId { get; set; } = new();

    public void PostAndSerialize()
    {
        SerializedSingletonRegistry.PostAndSerialize<Cache>(this, true);
    }

    private static Cache GetInstance()
    {
        if (SerializedSingletonRegistry.TryGetInstance<Cache>(out Cache? instance))
        {
            Debug.Assert(instance is not null);
            return instance;
        }

        Cache cache = new();
        SerializedSingletonRegistry.Post(cache);
        return cache;
    }
}