using System.Collections.Concurrent;
using System.Diagnostics;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Helper;

public class ConcurrentDownloader
{
    private const int ConcurrentDownloads = 10;

    private readonly GameResolver resolver;
    private readonly List<ISteamApp> apps;
    private int handledAppsTotal;
    private readonly ConcurrentDictionary<string, string> cachedNamesById;

    public ConcurrentDownloader(GameResolver resolver, List<ISteamApp> apps)
    {
        this.resolver = resolver;
        this.apps = apps;
        cachedNamesById = Cache.Instance.NamesByAppId;
    }

    public async Task ResolveAppNames()
    {
        List<Task<(ISteamApp, ApiResponse)>> apiResponseTasks =
            new List<Task<(ISteamApp, ApiResponse)>>(ConcurrentDownloads);

        // start x concurrent requests
        for (int concurrentTasks = 0; concurrentTasks < ConcurrentDownloads; concurrentTasks++)
        {
            ResolveAppsUntilOneRequestIsMade(apiResponseTasks);
        }

        // handle completed tasks
        // & start a new request for every task that completes
        while (apiResponseTasks.Count != 0)
        {
            // handle completed tasks one at a time 
            // though not necessarily on the same thread
            // removes need for synchronization beyond memory barriers
            // which are provided by await
            Task<(ISteamApp, ApiResponse)> completedTask = await Task.WhenAny(apiResponseTasks);
            apiResponseTasks.Remove(completedTask);

            // task is already completed; wait just to rethrow exceptions
            (ISteamApp app, ApiResponse response) = await completedTask;

            resolver.HandleApiResponse(app, response);

            ResolveAppsUntilOneRequestIsMade(apiResponseTasks);
        }
    }

    private void ResolveAppsUntilOneRequestIsMade(List<Task<(ISteamApp, ApiResponse)>> apiResponseTasks)
    {
        // return if all apps are handled
        while (handledAppsTotal < apps.Count)
        {
            ISteamApp nextApp = apps[handledAppsTotal];
            if (TryResolveByCache(nextApp))
            {
                continue;
            }

            // start request and return if an app cannot be resolved by cache
            ResolveByRequest(nextApp, apiResponseTasks);
            return;
        }
    }

    private bool TryResolveByCache(ISteamApp app)
    {
        bool resolvedByCache = resolver.TryResolveCached(app, cachedNamesById);
        if (resolvedByCache)
        {
            handledAppsTotal++;
        }

        return resolvedByCache;
    }

    private void ResolveByRequest(ISteamApp app, List<Task<(ISteamApp, ApiResponse)>> apiResponseTasks)
    {
        apiResponseTasks.Add(SteamApiClient.GetAppNameAsync(app));
        Debug.Assert(apiResponseTasks.Count() <= ConcurrentDownloads);
        handledAppsTotal++;
    }
}