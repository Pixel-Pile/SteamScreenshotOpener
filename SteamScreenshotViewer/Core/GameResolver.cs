using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Core;

public class GameResolver : ObservableObject
{
    private static ILogger log = Log.ForContext<GameResolver>();

    private ResolutionProgress? resolutionProgress;
    public event Action? AutoResolveFinishedPartialSuccess;
    public event Action? AutoResolveFinishedFullSuccess;
    public event Action? AutoResolveFailed;

    public readonly ICollection<ResolvedSteamApp> ResolvedApps = new List<ResolvedSteamApp>();
    public readonly ICollection<UnresolvedSteamApp> UnresolvedApps = new List<UnresolvedSteamApp>();
    private readonly List<string> knownDuplicateNames = new();

    /// <summary>
    /// Has to be called before <see cref="SearchAndResolveApps"/>
    /// to retrieve the instance of ResolutionProgress
    /// that can be used to monitor the resolution progress of this GameResolver.
    /// <br/>
    /// Every call to this method replaces this GameResolver's ResolutionProgress instance
    /// and returns the new instance,
    /// effectively resetting this GameResolver.
    /// </summary>
    public ResolutionProgress ResetResolutionProgress()
    {
        knownDuplicateNames.Clear();
        UnresolvedApps.Clear();
        ResolvedApps.Clear();
        resolutionProgress = new ResolutionProgress();
        return resolutionProgress;
    }

    private List<ISteamApp> SearchApps()
    {
        string[] screenshotPaths = Directory.EnumerateDirectories(Config.Instance.ScreenshotBasePath!).ToArray();
        List<ISteamApp> apps = new(screenshotPaths.Length);
        foreach (string path in screenshotPaths)
        {
            string appId = GetAppIdFromScreenshotDirectoryPath(path);
            apps.Add(new SteamApp(appId, path + @"\screenshots"));
        }

        resolutionProgress!.TotalAppCount = apps.Count;
        return apps;
    }

    public async Task SearchAndResolveApps()
    {
        _ = resolutionProgress
            ?? throw new InvalidOperationException(
                $"{nameof(ResetResolutionProgress)} must be called before {nameof(SearchAndResolveApps)}");

        List<ISteamApp> apps = SearchApps();
        ConcurrentDownloader concurrentDownloader = new ConcurrentDownloader(this, apps);
        try
        {
            await concurrentDownloader.ResolveAppNames();
        }
        catch (CancelRequestsException e)
        {
            log.Warning("requests were cancelled");
            AutoResolveFailed?.Invoke();
            return;
        }
        finally
        {
            Cache.Instance.PostAndSerialize();
        }

        if (UnresolvedApps.Count > 0)
        {
            AutoResolveFinishedPartialSuccess?.Invoke();
            return;
        }

        if (ResolvedApps.Count == apps.Count)
        {
            AutoResolveFinishedFullSuccess?.Invoke();
            return;
        }
    }


    public bool TryResolveCached(ISteamApp app, ConcurrentDictionary<string, string> cachedNamesById)
    {
        if (cachedNamesById.TryGetValue(app.Id, out string? name))
        {
            // cached
            ResolvedSteamApp resolvedApp = new ResolvedSteamApp(app, name);
            AddResolvedAppCandidate(resolvedApp, false);
            return true;
        }

        return false;
    }

    private void HandleNewlyResolvedApp(ResolvedSteamApp resolvedApp)
    {
        AddResolvedAppCandidate(resolvedApp, true);
        // Console.WriteLine($"resolved name: {resolvedApp.Id} -> {resolvedApp.Name}");
    }


    private bool IsUniqueAndNotEmpty(string name, out ResolvedSteamApp? duplicateToRemove)
    {
        if (string.IsNullOrEmpty(name))
        {
            duplicateToRemove = null;
            return false;
        }

        return IsUnique(name, out duplicateToRemove);
    }

    /// <summary>
    /// Returns whether the app has a unique name.
    /// </summary>
    /// <param name="duplicateToRemove">
    /// An app with the specified name that is considered resolved.
    /// Such an app does not necessarily exist, even if the name is not unique
    /// (duplicateToRemove can be null even if this method returns false).
    /// </param>
    /// <returns></returns>
    private bool IsUnique(string name, out ResolvedSteamApp? duplicateToRemove)
    {
        // name already detected as duplicate 
        // (this is at least the 3rd app with this name) 
        if (knownDuplicateNames.Contains(name))
        {
            duplicateToRemove = null;
            return false;
        }

        // search for earlier resolved app with same name
        duplicateToRemove =
            ResolvedApps.FirstOrDefault(app => string.Compare(name, app.Name, StringComparison.OrdinalIgnoreCase) == 0);
        if (duplicateToRemove is not null)
        {
            knownDuplicateNames.Add(name);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates an app's NameCandidate.
    /// Invalidates any duplicate NameCandidates.
    /// Revalidates duplicates of the previous NameCandidate.
    /// </summary>
    /// <param name="appToVerify">The app whose NameCandidate should be validated.</param>
    /// <param name="oldCleanedNameCandidate">The previous value of the app's CleanedNameCandidate.</param>
    public void ValidateNameCandidate(UnresolvedSteamApp appToVerify, string? oldCleanedNameCandidate)
    {
        if (string.IsNullOrEmpty(appToVerify.CleanedNameCandidate))
        {
            appToVerify.NameCandidateValid = false;
        }

        if (!IsUniqueAndNotEmpty(appToVerify.CleanedNameCandidate, out ResolvedSteamApp? _))
        {
            appToVerify.NameCandidateValid = false;
            return;
        }

        ValidateNameCandidateAgainstUnresolvedApps(appToVerify, oldCleanedNameCandidate);
    }

    /// <summary>
    /// Validates an app's NameCandidate against the NameCandidates of all unresolved apps.
    /// The result of this operation is stored in <see cref="UnresolvedSteamApp.NameCandidateValid"/>.
    /// <br />
    /// If any unresolved app has the same CandidateName, both apps have their CandidateName invalidated.
    /// If there is exactly 1 app whose NameCandidate used to be a duplicate of appToVerify's previous NameCandidate,
    /// it is revalidated.
    /// </summary>
    /// <param name="appToVerify">The app whose NameCandidate should be validated.</param>
    /// <param name="oldCleanedNameCandidate">The previous value of the app's CleanedNameCandidate.</param>
    private void ValidateNameCandidateAgainstUnresolvedApps(UnresolvedSteamApp appToVerify,
        string? oldCleanedNameCandidate)
    {
        int appsWithOldNameCandidateCount = 0;
        UnresolvedSteamApp? lastAppWithOldNameCandidate = null;
        bool unique = true;
        foreach (UnresolvedSteamApp currentApp in UnresolvedApps)
        {
            if (currentApp == appToVerify)
            {
                continue;
            }

            // search for unresolved apps whose nameCandidate is invalid
            // because it was a duplicate of the previous nameCandidate of appToVerify
            if (string.Compare(oldCleanedNameCandidate, currentApp.CleanedNameCandidate,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                appsWithOldNameCandidateCount++;
                lastAppWithOldNameCandidate = currentApp;
                // oldName != nameToVerify
                // currentName == oldName => currentName != nameToVerify
                // -> skip comparison with nameToVerify
                continue;
            }

            // search for duplicate nameCandidate
            if (string.Compare(appToVerify.CleanedNameCandidate, currentApp.CleanedNameCandidate,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                unique = false;
                currentApp.NameCandidateValid = false;
                appToVerify.NameCandidateValid = false;
            }
        }

        if (appsWithOldNameCandidateCount == 1)
        {
            if (IsUniqueAndNotEmpty(lastAppWithOldNameCandidate!.CleanedNameCandidate, out var _))
            {
                lastAppWithOldNameCandidate.NameCandidateValid = true;
            }
        }

        if (unique)
        {
            appToVerify.NameCandidateValid = true;
        }
    }

    /// <summary>
    /// Tries to add a resolved app.
    /// If resolvedApp.Name is unique: app is added to resolved apps.
    /// Otherwise: app name is indentified as duplicate and handled to unresolved apps.
    /// </summary>
    private void AddResolvedAppCandidate(ResolvedSteamApp resolvedApp, bool addToCache)
    {
        if (IsUniqueAndNotEmpty(resolvedApp.Name, out ResolvedSteamApp? duplicateToRemove))
        {
            AddResolved(resolvedApp, addToCache);
        }
        else
        {
            // name not unique
            AddUnresolved(new UnresolvedSteamApp(resolvedApp, FailureCause.DuplicateName, this));

            if (duplicateToRemove is not null)
            {
                RemoveResolved(duplicateToRemove);
                // remove duplicate from cache if present
                Cache.Instance.NamesByAppId.TryRemove(duplicateToRemove.Id, out string _);
                AddUnresolved(new UnresolvedSteamApp(duplicateToRemove, FailureCause.DuplicateName, this));
            }
        }
    }

    public void HandleApiResponse(ISteamApp app, ApiResponse response)
    {
        switch (response.ResponseState)
        {
            case ResponseState.Success:
                HandleNewlyResolvedApp(new ResolvedSteamApp(app, response.Name!));
                break;
            case ResponseState.FailureSkipApp:
                Debug.Assert(response.FailureCause != null);
                AddUnresolved(new UnresolvedSteamApp(app, response.FailureCause!.Value, this));
                break;
            case ResponseState.CancelAll:
                AddUnresolved(new UnresolvedSteamApp(app, response.FailureCause!.Value, this));
                break;
            case ResponseState.FailureRetryAppWithDifferentFilters:
                log.Error(
                    $"ResponseState was {ResponseState.FailureRetryAppWithDifferentFilters} in {nameof(HandleApiResponse)}");
                Debug.Assert(false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private string GetAppIdFromScreenshotDirectoryPath(string directory)
    {
        int idStart = directory.LastIndexOf(@"\") + 1;
        string id = directory.Substring(idStart, directory.Length - idStart);
        if (id is null)
        {
            throw new NullReferenceException("id is null for path: " + directory);
        }

        return id;
    }


    public void RemoveUnresolved(UnresolvedSteamApp unresolvedApp)
    {
        UnresolvedApps.Remove(unresolvedApp);
        resolutionProgress!.UnresolvedAppCount--;
    }

    public void AddUnresolved(UnresolvedSteamApp unresolvedApp)
    {
        UnresolvedApps.Add(unresolvedApp);
        resolutionProgress!.UnresolvedAppCount++;
    }

    public void RemoveResolved(ResolvedSteamApp resolvedApp)
    {
        ResolvedApps.Remove(resolvedApp);
        resolutionProgress!.ResolvedAppCount--;
    }

    public void AddResolved(ResolvedSteamApp resolvedApp, bool addToCache)
    {
        ResolvedApps.Add(resolvedApp);
        resolutionProgress!.ResolvedAppCount++;
        if (addToCache)
        {
            if (!Cache.Instance.NamesByAppId.TryAdd(resolvedApp.Id, resolvedApp.Name))
            {
                throw new InvalidOperationException("attempted to overwrite name entry in cache");
            }
        }
    }
}