using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
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
    private static readonly ILogger log = Log.ForContext<Conductor>();
    private const int NetworkFailureThreshold = 1; //TODO reset to 5

    private GameResolver gameResolver = new();
    public event EventHandler<PromptForScreenshotPathEventArgs>? PromptForScreenshotPath;
    public event Action? AutoResolveStarted;
    public event Action? ResolveManually;

    #region pseudo-redeclared gameResolver events

    private event Action? _autoResolveCompleted;

    public event Action? AutoResolveCompleted
    {
        add
        {
            _autoResolveCompleted += value;
            gameResolver.AutoResolveCompleted += value;
        }
        remove
        {
            _autoResolveCompleted -= value;
            gameResolver.AutoResolveCompleted -= value;
        }
    }

    private event Action? _networkFailed;

    public event Action? NetworkFailed
    {
        add
        {
            _networkFailed += value;
            gameResolver.NetworkFailed += value;
        }
        remove
        {
            _networkFailed -= value;
            gameResolver.NetworkFailed -= value;
        }
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
        gameResolver.AutoResolveCompletedWithFailures += HandleAutoResolveCompletedWithFailures;
    }

    private void HandleAutoResolveCompletedWithFailures()
    {
        int networkFailureCount = UnresolvedApps.Count(app => app.FailureCause == FailureCause.Network);

        if (networkFailureCount >= NetworkFailureThreshold)
        {
            log.Information($"exceeded {nameof(NetworkFailureThreshold)}, resolution deemed network failure");
            NonNull.InvokeEvent(_networkFailed);
        }
        else
        {
            ResolveFailuresManually();
        }
    }

    public void ResolveFailuresManually()
    {
        ObservableUnresolvedApps =
            new ObservableCollection<UnresolvedSteamApp>(gameResolver.UnresolvedApps);
        NonNull.InvokeEvent(ResolveManually);
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
                _autoResolveCompleted?.Invoke();
            }
        }
    }
}