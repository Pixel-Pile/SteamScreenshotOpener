using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Helper;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Core;

public class PromptForScreenshotPathEventArgs
{
    /// <summary>
    /// The callback that should be invoked with the game-specific screenshot path.
    /// </summary>
    public readonly Action<string> SetScreenshotPathCallback;

    public PromptForScreenshotPathEventArgs(Action<string> setScreenshotPathCallback)
    {
        SetScreenshotPathCallback = setScreenshotPathCallback;
    }
}

[INotifyPropertyChanged]
public partial class Conductor
{
    private GameResolver gameResolver = new();
    public event EventHandler<PromptForScreenshotPathEventArgs>? PromptForScreenshotPath;
    public event Action? AutoResolveStarted;

    #region redeclared gameResolver events

    public event Action? AutoResolveFinishedPartialSuccess
    {
        add { gameResolver.AutoResolveFinishedPartialSuccess += value; }
        remove { gameResolver.AutoResolveFinishedPartialSuccess -= value; }
    }

    private event Action? _autoResolveFinishedFullSuccess;

    public event Action? AutoResolveFinishedFullSuccess
    {
        add
        {
            _autoResolveFinishedFullSuccess += value;
            gameResolver.AutoResolveFinishedFullSuccess += value;
        }
        remove
        {
            _autoResolveFinishedFullSuccess -= value;
            gameResolver.AutoResolveFinishedFullSuccess -= value;
        }
    }

    public event Action? AutoResolveFailed
    {
        add { gameResolver.AutoResolveFailed += value; }
        remove { gameResolver.AutoResolveFailed -= value; }
    }

    #endregion

    #region redeclared gameresolver members

    [ObservableProperty] private ResolutionProgress resolutionProgress;
    public ObservableCollection<UnresolvedSteamApp> ObservableUnresolvedApps { get; private set; }
    public ICollection<ResolvedSteamApp> ResolvedApps => gameResolver.ResolvedApps;
    public ICollection<UnresolvedSteamApp> UnresolvedApps => gameResolver.UnresolvedApps;

    #endregion

    public Conductor()
    {
        gameResolver.AutoResolveFinishedPartialSuccess += () =>
        {
            this.ObservableUnresolvedApps =
                new ObservableCollection<UnresolvedSteamApp>(gameResolver.UnresolvedApps);
        };
    }

    public void Start()
    {
        ResolutionProgress = gameResolver.ResetResolutionProgress();
        Config config = Config.Instance;
        if (config.ScreenshotBasePath is null)
        {
            NonNull.InvokeEvent(PromptForScreenshotPath,
                new PromptForScreenshotPathEventArgs(HandleGameSpecificPathSubmitted));
            return;
        }

        StartAutoResolve();
    }

    private void StartAutoResolve()
    {
        NonNull.InvokeEvent(AutoResolveStarted);
        LoadAppList();
    }

    private void HandleGameSpecificPathSubmitted(string gameSpecificScreenshotPath)
    {
        Config config = Config.Instance;
        config.ScreenshotBasePath = PathHelper.ResolveScreenshotBasePath(gameSpecificScreenshotPath);
        config.PostAndSerialize();
        StartAutoResolve();
    }

    private void LoadAppList()
    {
        TaskHelper.Run(gameResolver.SearchAndResolveApps);
    }

    public void RetryAutoResolve()
    {
        ResolutionProgress = gameResolver.ResetResolutionProgress();
        // PropertyChanged?.Invoke(this, new(null));
        StartAutoResolve();
    }

    public void ResolveAppIfNameCandidateValid(UnresolvedSteamApp unresolvedApp)
    {
        // if null or false: do nothing
        if (unresolvedApp.NameCandidateValid == true)
        {
            ResolvedSteamApp resolvedApp = new ResolvedSteamApp(unresolvedApp, unresolvedApp.CleanedNameCandidate);
            gameResolver.RemoveUnresolved(unresolvedApp);
            gameResolver.AddResolved(resolvedApp, true);
            ObservableUnresolvedApps.Remove(unresolvedApp);
            if (UnresolvedApps.Count == 0)
            {
                Cache.Instance.PostAndSerialize();
                _autoResolveFinishedFullSuccess?.Invoke();
            }
        }
    }
}