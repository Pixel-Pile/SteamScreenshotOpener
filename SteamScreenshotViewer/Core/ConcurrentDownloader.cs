using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Core;

public class ConcurrentDownloader
{
    private static ILogger log = Log.ForContext<ConcurrentDownloader>();
    private const int ConcurrentDownloads = 10;
    private const int DistinctFailureLimit = 3;

    private readonly GameResolver resolver;
    
    private readonly List<ISteamApp> apps;
    private readonly ConcurrentDictionary<string, string> cachedNamesById;
    private int handledAppsTotal;
    
    private int distinctFailureCount;
    private DateTime distinctFailureTime = DateTime.MinValue;
    private static readonly TimeSpan FailureDelay = TimeSpan.FromMilliseconds(500);

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

            if (response.ResponseState == ResponseState.CancelAll)
            {
                await StallThrowIfRepeatedFailure(response);
            }

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
        apiResponseTasks.Add(SteamApiWrapper.GetAppNameAsync(app));
        Debug.Assert(apiResponseTasks.Count() <= ConcurrentDownloads);
        handledAppsTotal++;
    }

    private bool IsDistinctFailure(ApiResponse apiResponse)
    {
        if (apiResponse.ResponseState != ResponseState.CancelAll)
        {
            throw new ArgumentException($"ApiResponse did not represent failure but '{apiResponse.ResponseState}'");
        }

        if (apiResponse.TimeStamp > distinctFailureTime + FailureDelay)
        {
            distinctFailureTime = DateTime.Now;
            return true;
        }

        return false;
    }


    /// <summary>
    /// If at least <see cref="FailureDelay"/> milliseconds have passed since the last distinct failure:
    /// <list type="bullet">
    /// <item>the current failure is deemed a new distinct failure</item>
    /// <item><see cref="distinctFailureTime"/> is updated</item>
    /// <item>stalls the current thread for <see cref="FailureDelay"/> milliseconds</item>
    /// </list>
    /// </summary>
    /// <param name="apiResponse"></param>
    /// <exception cref="CancelRequestsException"></exception>
    private async Task StallThrowIfRepeatedFailure(ApiResponse apiResponse)
    {
        if (!IsDistinctFailure(apiResponse))
        {
            return;
        }

        log.Information("distinct failure detected");
        distinctFailureCount++;
        if (distinctFailureCount >= DistinctFailureLimit)
        {
            throw new CancelRequestsException(
                $"cancel request limit ({DistinctFailureLimit}) reached (network failure)");
        }

        await Task.Delay(FailureDelay);
    }
}