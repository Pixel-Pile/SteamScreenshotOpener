using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Core;

public partial class ResolutionProgress : ObservableObject
{
    // public ObservableCollection<UnresolvedSteamApp> ObservedUnresolvedApps { get; set; } = new();

    [ObservableProperty] private int totalAppCount;
    [ObservableProperty] private int resolvedAppCount;
    [ObservableProperty] private int unresolvedAppCount;
    [ObservableProperty] private double autoResolvingProgress;

    partial void OnTotalAppCountChanged(int value) => UpdateAutoResolveProgress();
    partial void OnResolvedAppCountChanged(int value) => UpdateAutoResolveProgress();
    partial void OnUnresolvedAppCountChanged(int value) => UpdateAutoResolveProgress();

    private void UpdateAutoResolveProgress()
    {
        int unhandledOrResolvedAppCount = TotalAppCount - UnresolvedAppCount;
        if (unhandledOrResolvedAppCount == 0)
        {
            AutoResolvingProgress = 0;
            return;
        }

        AutoResolvingProgress = ResolvedAppCount * 100 / (double)unhandledOrResolvedAppCount;
    }
}