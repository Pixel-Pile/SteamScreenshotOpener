using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Views;

public partial class ViewUnresolvedApps : TopLevelView
{
    public ViewUnresolvedApps(GameResolver resolver)
    {
        GameResolver = resolver;
        InitializeComponent();
    }

    [ObservableProperty] private GameResolver gameResolver;

    private void DataGrid_OnRowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
    {
        Console.WriteLine(e.GetType());
    }

    private void Commit(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: UnresolvedSteamApp unresolvedApp })
        {
            GameResolver.ResolveAppIfNameCandidateValid(unresolvedApp);
        }
    }

    private void CommitAll(object sender, RoutedEventArgs e)
    {
        UnresolvedSteamApp[] unresolvedApps = GameResolver.UnresolvedApps.ToArray();
        foreach (UnresolvedSteamApp unresolvedApp in unresolvedApps)
        {
            GameResolver.AttemptManualResolve(unresolvedApp, unresolvedApp.NameCandidate);
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
            GameResolver.ResolveAppIfNameCandidateValid(unresolvedApp);
        }
    }
}