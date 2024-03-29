using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Core;

namespace SteamScreenshotViewer.Views;

public partial class ViewNetworkFailure : TopLevelView
{
    public ViewNetworkFailure(Conductor conductor)
    {
        this.Conductor = conductor;
        InitializeComponent();
    }

    [ObservableProperty] private Conductor conductor;
    [ObservableProperty] private string apiResponse;

    private void TestConnection(object sender, RoutedEventArgs e)
    {
        //TODO
        throw new NotImplementedException();
    }

    private void RetryAutoResolve(object sender, RoutedEventArgs e)
    {
        //TODO
        throw new NotImplementedException();
    }
}