using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Core;
using SteamScreenshotViewer.Model;
using GameResolver = SteamScreenshotViewer.Core.GameResolver;

namespace SteamScreenshotViewer.Views;

public partial class ViewLoadingScreen : TopLevelView
{
    public ViewLoadingScreen(Conductor conductor)
    {
        Conductor = conductor;
        InitializeComponent();
    }

    [ObservableProperty] private Conductor conductor;
}