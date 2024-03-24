﻿using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Helper;

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
        await concurrentDownloader.ResolveAppNames();
        Thread.MemoryBarrier();
        AutoResolveFinished?.Invoke();
        if (UnresolvedApps.Count == 0)
        {
            AppsFullyResolved?.Invoke();
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


    /// <summary>
    /// Returns whether the app has a unique name.
    /// </summary>
    /// <param name="duplicateToRemove">Can be null even if this method returns false.
    /// Contains the app with the same name that was considered resolved until now.
    /// </param>
    /// <returns></returns>
    public bool IsUnique(string name, out ResolvedSteamApp? duplicateToRemove)
    {
        if (string.IsNullOrEmpty(name) || name.Trim().Length == 0)
        {
            duplicateToRemove = null;
            return false;
        }

        name = name.ToLower().Trim();
        // name already detected as duplicate 
        // (this is at least the 3rd app with this name) 
        if (knownDuplicateNames.Contains(name))
        {
            duplicateToRemove = null;
            return false;
        }

        // search for earlier resolved app with same name
        duplicateToRemove = ResolvedApps.FirstOrDefault(app => app.LowerCaseName == name);
        if (duplicateToRemove is not null)
        {
            knownDuplicateNames.Add(name);
            return false;
        }

        return true;
    }

    public void ValidateNameCandidate(UnresolvedSteamApp appToVerify)
    {
        if (!IsUnique(appToVerify.NameCandidate, out ResolvedSteamApp? _))
        {
            appToVerify.NameCandidateValid = false;
            return;
        }

        foreach (UnresolvedSteamApp app in UnresolvedApps)
        {
            if (app.NameCandidate.ToLower().Trim() == appToVerify.NameCandidate.ToLower().Trim())
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


    public void HandleApiResponse(ISteamApp app, ApiResponse response)
    {
        if (!response.ContainsName)
        {
            Debug.Assert(response.FailureCause != null);
            HandleUnresolvedApp(new UnresolvedSteamApp(app, response.FailureCause!.Value, this));
            return;
        }

        // name found
        HandleNewlyResolvedApp(new ResolvedSteamApp(app, response.Name!));
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

    public bool AttemptManualResolve(UnresolvedSteamApp unresolvedApp, string unresolvedAppName)
    {
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

    public void ResolveAppIfNameCandidateValid(UnresolvedSteamApp unresolvedApp)
    {
        // null or false
        if (unresolvedApp.NameCandidateValid != true)
        {
            return;
        }

        AttemptManualResolve(unresolvedApp, unresolvedApp.NameCandidate);
    }
}