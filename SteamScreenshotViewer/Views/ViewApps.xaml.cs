using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Views;

public partial class ViewApps : TopLevelView
{
    public ViewApps()
    {
        InitializeComponent();
    }

    [ObservableProperty] private GameResolver gameResolver;


    private void OnAppClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: ResolvedSteamApp steamApp })
        {
            Process.Start("explorer.exe", steamApp.ScreenshotsPath);
        }
    }
}