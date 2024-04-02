using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Core;
using SteamScreenshotViewer.Model;
using GameResolver = SteamScreenshotViewer.Core.GameResolver;

namespace SteamScreenshotViewer.Views;

public partial class ViewUnresolvedApps : TopLevelView
{
    public ViewUnresolvedApps(Conductor conductor)
    {
        Conductor = conductor;
        InitializeComponent();
    }

    [ObservableProperty] private Conductor conductor;


    private void Commit(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: UnresolvedSteamApp unresolvedApp })
        {
            Conductor.ResolveAppIfNameCandidateValid(unresolvedApp);
        }
    }

    private void CommitAll(object sender, RoutedEventArgs e)
    {
        UnresolvedSteamApp[] unresolvedApps = Conductor.UnresolvedApps.ToArray();
        foreach (UnresolvedSteamApp unresolvedApp in unresolvedApps)
        {
            Conductor.ResolveAppIfNameCandidateValid(unresolvedApp);
        }
    }


    private void OnKeyDownHandler(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return)
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: UnresolvedSteamApp unresolvedApp } elem)

        {
            // focus next item before resolving = potentially removing current
            TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Down);
            request.Wrapped = true;
            elem.MoveFocus(request);
            Conductor.ResolveAppIfNameCandidateValid(unresolvedApp);
        }
    }
}