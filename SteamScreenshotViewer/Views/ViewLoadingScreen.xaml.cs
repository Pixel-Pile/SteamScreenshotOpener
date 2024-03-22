using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Views;

[INotifyPropertyChanged]
public partial class ViewLoadingScreen : UserControl
{
    public ViewLoadingScreen()
    {
        InitializeComponent();
    }

    [ObservableProperty] private GameResolver gameResolver;
}