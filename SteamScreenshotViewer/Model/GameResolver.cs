using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SteamScreenshotViewer.Model;

public partial class GameResolver : ObservableObject
{
    //TODO inotify required for the collections? (would signal replacement of entire data structure)

    public ObservableCollection<ResolvedSteamApp> ObservedResolvedApps { get; set; } = new();
    public ObservableCollection<UnresolvedSteamApp> ObservedUnresolvedApps { get; set; } = new();

    [ObservableProperty] private int totalAppCount;
    [ObservableProperty] private double autoResolvingProgress;
    public event Action? AutoResolveFinished;
    public event Action? AppsFullyResolved;

    public ICollection<ResolvedSteamApp> ResolvedApps = new List<ResolvedSteamApp>();
    public ICollection<UnresolvedSteamApp> UnresolvedApps = new List<UnresolvedSteamApp>();
    private List<ISteamApp> apps = new();
    private List<string> knownDuplicateNames = new();

    //FIXME when to instantiate and dispose HttpClient
    HttpClient httpClient = new();


    public ICollection<ISteamApp> SearchApps()
    {
        apps = new();
        foreach (string path in Directory.EnumerateDirectories(Config.Instance.ScreenshotBasePath))
        {
            string appId = GetAppIdFromScreenshotDirectoryPath(path);
            apps.Add(new SteamApp(appId, path + @"\screenshots"));
        }

        TotalAppCount = apps.Count;
        return apps;
    }

    public async Task ResolveAppNames()
    {
        ConcurrentDictionary<string, string> cachedNamesByIdCopy = Cache.Instance.NamesByAppId;
        foreach (ISteamApp app in apps)
        {
            if (cachedNamesByIdCopy.ContainsKey(app.Id))
            {
                // cached
                ResolvedSteamApp resolvedApp = new ResolvedSteamApp(app, cachedNamesByIdCopy[app.Id]);
                AddResolvedAppCandidate(resolvedApp, false);
                Console.WriteLine($"already cached: {resolvedApp}");
            }
            else
            {
                //not cached
                await AutoResolveApp(app);
            }
        }

        Thread.MemoryBarrier();
        AutoResolveFinished?.Invoke();
        if (UnresolvedApps.Count == 0)
        {
            AppsFullyResolved?.Invoke();
        }

        Cache.Instance.PostAndSerialize();
    }

    private void HandleUnresolvedApp(UnresolvedSteamApp unresolvedApp)
    {
        AddUnresolved(unresolvedApp);
        Console.WriteLine("could not resolve name for id: " + unresolvedApp.Id);
    }


    private void HandleNewlyResolvedApp(ResolvedSteamApp resolvedApp)
    {
        AddResolvedAppCandidate(resolvedApp, true);
        Console.WriteLine($"resolved name: {resolvedApp.Id} -> {resolvedApp.Name}");
    }


    /// <summary>
    /// Returns whether the app has a unique name.
    /// </summary>
    /// <param name="duplicateToRemove">Can be null even if this method returns false.
    /// Contains the app with the same name that was considered resolved until now.
    /// </param>
    /// <returns></returns>
    public bool IsUnique(string name, out ResolvedSteamApp? duplicateToRemove)
    {
        // name already detected as duplicate 
        // (this is at least the 3rd app with this name) 
        if (knownDuplicateNames.Contains(name))
        {
            duplicateToRemove = null;
            return false;
        }

        // search for earlier resolved app with same name
        duplicateToRemove = ResolvedApps.FirstOrDefault(app => app.Name == name);
        if (duplicateToRemove is not null)
        {
            knownDuplicateNames.Add(name);
            return false;
        }

        return true;
    }

    public void ValidateNameCandidate(UnresolvedSteamApp appToVerify)
    {
        if (string.IsNullOrEmpty(appToVerify.NameCandidate))
        {
            appToVerify.NameCandidateValid = false;
            return;
        }

        if (!IsUnique(appToVerify.NameCandidate, out ResolvedSteamApp? _))
        {
            appToVerify.NameCandidateValid = false;
            return;
        }

        foreach (UnresolvedSteamApp app in UnresolvedApps)
        {
            if (app.NameCandidate == appToVerify.NameCandidate)
            {
                if (app == appToVerify)
                {
                    continue;
                }

                app.NameCandidateValid = false;
                appToVerify.NameCandidateValid = false;
                return;
            }
        }

        appToVerify.NameCandidateValid = true;
    }

    /// <summary>
    /// Tries to add a resolved app.
    /// If resolvedApp.Name is unique: app is added to resolved apps.
    /// Otherwise: app name is indentified as duplicate and handled to unresolved apps.
    /// </summary>
    private void AddResolvedAppCandidate(ResolvedSteamApp resolvedApp, bool addToCache)
    {
        if (IsUnique(resolvedApp.Name, out ResolvedSteamApp? duplicateToRemove))
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


    public async Task AutoResolveApp(ISteamApp app)
    {
        try
        {
            string? name = await GetAppNameAsync(app.Id);

            if (name is null)
            {
                HandleUnresolvedApp(new UnresolvedSteamApp(app, FailureCause.SteamApi, this));
                return;
            }

            // name found
            HandleNewlyResolvedApp(new ResolvedSteamApp(app, name));
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"request for {app.Id} failed with code {e.StatusCode}");
            Console.WriteLine(e.StackTrace);
            HandleUnresolvedApp(new UnresolvedSteamApp(app, FailureCause.Network, this));
        }
    }

    public async Task<string?> GetAppNameAsync(string appId)
    {
        string? response = null;
        try
        {
            response = await httpClient.GetStringAsync(
                $"https://store.steampowered.com/api/appdetails??filter=basic&appids={appId}");
        }
        catch (TaskCanceledException timeout)
        {
            throw new HttpRequestException("request failed due to timeout", timeout);
        }

        JsonNode responseJson = JsonObject.Parse(response)[appId];
        if (responseJson["success"].ToString() == "false")
        {
            return null;
        }

        JsonNode appData = responseJson["data"];
        return appData["name"].ToString();
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
        AutoResolvingProgress = ObservedResolvedApps.Count * 100 / (double)(TotalAppCount - ObservedUnresolvedApps.Count);
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

    public bool AttemptManualResolve(UnresolvedSteamApp unresolvedApp, string unresolvedAppName)
    {
        ValidateNameCandidate(unresolvedApp);
        switch (unresolvedApp.NameCandidateValid)
        {
            case true:
                ResolvedSteamApp resolvedApp = new ResolvedSteamApp(unresolvedApp, unresolvedApp.NameCandidate);
                RemoveUnresolved(unresolvedApp);
                AddResolved(resolvedApp, true);
                if (UnresolvedApps.Count == 0)
                {
                    Cache.Instance.PostAndSerialize();
                    AppsFullyResolved?.Invoke();
                }

                return true;
            case false:
                // different duplicate handling from AddResolvedAppCandidate
                // (existing resolved app with same name is not invalidated
                // but resolving this app is denied)
                return false;
            case null:
                throw new InvalidOperationException(
                    $"{nameof(UnresolvedSteamApp.NameCandidateValid)} was null after call to {nameof(ValidateNameCandidate)}");
        }
    }
}