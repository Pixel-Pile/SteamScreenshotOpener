using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Views;

[INotifyPropertyChanged]
public partial class ViewApps : UserControl
{
    public ViewApps()
    {
        InitializeComponent();
    }

    [ObservableProperty] private GameResolver gameResolver;


    private void OnAppClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as ListViewItem)?.Content is ISteamApp steamApp)
        {
            Process.Start("explorer.exe", steamApp.ScreenshotsPath);
        }
    }
}