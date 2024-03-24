using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Views;

public partial class ViewUnresolvedApps : TopLevelView
{
    public ViewUnresolvedApps()
    {
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
            if (unresolvedApp.NameCandidateValid is false)
            {
                return;
            }

            GameResolver.AttemptManualResolve(unresolvedApp, unresolvedApp.NameCandidate);
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

}