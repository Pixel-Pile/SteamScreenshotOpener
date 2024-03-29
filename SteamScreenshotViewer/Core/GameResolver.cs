using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Core;

public partial class GameResolver : ObservableObject
{
    //TODO inotify required for the collections? (would signal replacement of entire data structure)

    private static ILogger log = Log.ForContext<GameResolver>();
    public ObservableCollection<ResolvedSteamApp> ObservedResolvedApps { get; set; } = new();
    public ObservableCollection<UnresolvedSteamApp> ObservedUnresolvedApps { get; set; } = new();

    [ObservableProperty] private int totalAppCount;
    [ObservableProperty] private double autoResolvingProgress;

    public event Action? AutoResolveFinishedPartialSuccess;
    public event Action? AutoResolveFinishedFullSuccess;
    public event Action? AutoResolveFailed;

    public ICollection<ResolvedSteamApp> ResolvedApps = new List<ResolvedSteamApp>();
    public ICollection<UnresolvedSteamApp> UnresolvedApps = new List<UnresolvedSteamApp>();
    private List<string> knownDuplicateNames = new();


    private List<ISteamApp> SearchApps()
    {
        string[] screenshotPaths = Directory.EnumerateDirectories(Config.Instance.ScreenshotBasePath).ToArray();
        List<ISteamApp> apps = new(screenshotPaths.Count());
        foreach (string path in screenshotPaths)
        {
            string appId = GetAppIdFromScreenshotDirectoryPath(path);
            apps.Add(new SteamApp(appId, path + @"\screenshots"));
        }

        TotalAppCount = apps.Count;
        return apps;
    }

    public async Task SearchAndResolveApps()
    {
        List<ISteamApp> apps = SearchApps();
        ConcurrentDownloader concurrentDownloader = new ConcurrentDownloader(this, apps);
        try
        {
            await concurrentDownloader.ResolveAppNames();
        }
        catch (CancelRequestsException e)
        {
            log.Warning("requests were cancelled");
            Cache.Instance.PostAndSerialize();
            AutoResolveFailed?.Invoke();
            return;
        }

        AutoResolveFinishedPartialSuccess?.Invoke();
        if (UnresolvedApps.Count == 0)
        {
            AutoResolveFinishedFullSuccess?.Invoke();
        }

        Cache.Instance.PostAndSerialize();
    }

    public bool TryResolveCached(ISteamApp app, ConcurrentDictionary<string, string> cachedNamesById)
    {
        if (cachedNamesById.TryGetValue(app.Id, out string? name))
        {
            // cached
            ResolvedSteamApp resolvedApp = new ResolvedSteamApp(app, name);
            AddResolvedAppCandidate(resolvedApp, false);
            // Console.WriteLine($"already cached: {resolvedApp}");
            return true;
        }

        return false;
    }

    private void HandleUnresolvedApp(UnresolvedSteamApp unresolvedApp)
    {
        AddUnresolved(unresolvedApp);
        // Console.WriteLine("could not resolve name for id: " + unresolvedApp.Id);
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


    public async Task HandleApiResponse(ISteamApp app, ApiResponse response)
    {
        switch (response.ResponseState)
        {
            case ResponseState.Success:
                // name found
                HandleNewlyResolvedApp(new ResolvedSteamApp(app, response.Name!));
                break;
            case ResponseState.FailureSkipApp:
                Debug.Assert(response.FailureCause != null);
                HandleUnresolvedApp(new UnresolvedSteamApp(app, response.FailureCause!.Value, this));
                break;
            case ResponseState.FailureRetryApp:
                log.Error($"ResponseState was {ResponseState.FailureRetryApp} in {nameof(HandleApiResponse)}");
                Debug.Assert(false);
                break;
            case ResponseState.CancelAll:
                await StallButThrowIfRepeatedFailure();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private int cancelRequestCount = 0;
    private const int CancelRequestLimit = 3;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="milliseconds"></param>
    /// <exception cref="CancelRequestsException"></exception>
    private async Task StallButThrowIfRepeatedFailure(int milliseconds = 1000)
    {
        cancelRequestCount++;

        if (cancelRequestCount >= CancelRequestLimit)
        {
            throw new CancelRequestsException("cancel request limit reached (network failure)");
        }

        // stall/wait before trying next app
        await Task.Delay(milliseconds);
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

    private void UpdateAutoResolveProgress()
    {
        AutoResolvingProgress =
            ObservedResolvedApps.Count * 100 / (double)(TotalAppCount - ObservedUnresolvedApps.Count);
    }

    private void RemoveUnresolved(UnresolvedSteamApp unresolvedApp)
    {
        UnresolvedApps.Remove(unresolvedApp);
        Application.Current.Dispatcher.Invoke(() => ObservedUnresolvedApps.Remove(unresolvedApp));
    }

    private void AddUnresolved(UnresolvedSteamApp unresolvedApp)
    {
        UnresolvedApps.Add(unresolvedApp);
        Application.Current.Dispatcher.Invoke(() => ObservedUnresolvedApps.Add(unresolvedApp));
    }

    private void RemoveResolved(ResolvedSteamApp resolvedApp)
    {
        ResolvedApps.Remove(resolvedApp);
        Application.Current.Dispatcher.Invoke(() =>
        {
            ObservedResolvedApps.Remove(resolvedApp);
            UpdateAutoResolveProgress();
        });
    }


    private void AddResolved(ResolvedSteamApp resolvedApp, bool addToCache)
    {
        ResolvedApps.Add(resolvedApp);
        Application.Current.Dispatcher.Invoke(() =>
        {
            ObservedResolvedApps.Add(resolvedApp);
            UpdateAutoResolveProgress();
        });
        if (addToCache)
        {
            if (!Cache.Instance.NamesByAppId.TryAdd(resolvedApp.Id, resolvedApp.Name))
            {
                throw new InvalidOperationException("attempted to overwrite name entry in cache");
            }
        }
    }

    public bool AttemptManualResolve(UnresolvedSteamApp unresolvedApp)
    {
        switch (unresolvedApp.NameCandidateValid)
        {
            case true:
                ResolvedSteamApp resolvedApp = new ResolvedSteamApp(unresolvedApp, unresolvedApp.CleanedNameCandidate);
                RemoveUnresolved(unresolvedApp);
                AddResolved(resolvedApp, true);
                if (UnresolvedApps.Count == 0)
                {
                    Cache.Instance.PostAndSerialize();
                    AutoResolveFinishedFullSuccess?.Invoke();
                }

                return true;
            case false:
                // different duplicate handling from AddResolvedAppCandidate
                // (existing resolved app with same name is not invalidated
                // but resolving this app is denied)
                return false;
            case null:
                throw new InvalidOperationException(
                    $"{nameof(UnresolvedSteamApp.NameCandidateValid)} was null");
        }
    }

    public void ResolveAppIfNameCandidateValid(UnresolvedSteamApp unresolvedApp)
    {
        // null or false
        if (unresolvedApp.NameCandidateValid != true)
        {
            return;
        }

        AttemptManualResolve(unresolvedApp);
    }
}