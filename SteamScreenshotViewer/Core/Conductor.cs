using System.Collections.ObjectModel;
using SteamScreenshotViewer.Helper;
using SteamScreenshotViewer.Model;
using SteamScreenshotViewer.Views;

namespace SteamScreenshotViewer.Core;

public class Conductor
{
    private GameResolver gameResolver = new();

    public event Action? PromptForBasePath;
    public event Action? AutoResolveStarted;

    #region redeclared gameResolver events

    public event Action? AutoResolveFinishedPartialSuccess
    {
        add { gameResolver.AutoResolveFinishedPartialSuccess += value; }
        remove { gameResolver.AutoResolveFinishedPartialSuccess -= value; }
    }

    public event Action? AutoResolveFinishedFullSuccess
    {
        add { gameResolver.AutoResolveFinishedFullSuccess += value; }
        remove { gameResolver.AutoResolveFinishedFullSuccess -= value; }
    }

    public event Action? AutoResolveFailed
    {
        add { gameResolver.AutoResolveFailed += value; }
        remove { gameResolver.AutoResolveFailed -= value; }
    }

    #endregion

    #region redeclared gameresolver members

    public ObservableCollection<ResolvedSteamApp> ObservedResolvedApps => gameResolver.ObservedResolvedApps;
    public ObservableCollection<UnresolvedSteamApp> ObservedUnresolvedApps => gameResolver.ObservedUnresolvedApps;
    public int TotalAppCount => gameResolver.TotalAppCount;
    public double AutoResolvingProgress => gameResolver.AutoResolvingProgress;
    public ICollection<UnresolvedSteamApp> UnresolvedApps => gameResolver.UnresolvedApps;

    #endregion

    public void Start()
    {
        Config config = Config.Instance;
        if (config.ScreenshotBasePath is null)
        {
            NonNull.InvokeEvent(PromptForBasePath);
            return;
        }

        NonNull.InvokeEvent(AutoResolveStarted);
        LoadAppList();
    }

    public void HandleGameSpecificPathSubmitted(string gameSpecificScreenshotPath)
    {
        Config config = Config.Instance;
        config.ScreenshotBasePath = PathHelper.ResolveScreenshotBasePath(gameSpecificScreenshotPath);
        config.PostAndSerialize();
        NonNull.InvokeEvent(AutoResolveStarted);
        LoadAppList();
    }

    private void LoadAppList()
    {
        TaskHelper.Run(gameResolver.SearchAndResolveApps);
    }

    //TODO manual resolve methods have to change with network failure handling
    public void ResolveAppIfNameCandidateValid(UnresolvedSteamApp unresolvedApp)
    {
        gameResolver.ResolveAppIfNameCandidateValid(unresolvedApp);
    }

    public void AttemptManualResolve(UnresolvedSteamApp unresolvedApp)
    {
        gameResolver.AttemptManualResolve(unresolvedApp);
    }
}