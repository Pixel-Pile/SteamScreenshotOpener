﻿using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;

namespace SteamScreenshotViewer.Core;

public class Cache
{
    private static ILogger log = Log.ForContext<Cache>();

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

        log.Information("creating new cache instance");
        Cache cache = new();
        SerializedSingletonRegistry.Post(cache);
        return cache;
    }
}