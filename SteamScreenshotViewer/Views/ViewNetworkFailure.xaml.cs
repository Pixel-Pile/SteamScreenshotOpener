﻿using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Core;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Views;

public partial class ViewNetworkFailure : TopLevelView
{
    /// <summary>
    /// AppId of Portal
    /// </summary>
    private const string ConnectionTestAppId = "400";

    public ViewNetworkFailure(Conductor conductor)
    {
        this.Conductor = conductor;
        InitializeComponent();
    }

    [ObservableProperty] private Conductor conductor;
    [ObservableProperty] private string apiResponse;

    private void TestConnection(object sender, RoutedEventArgs e)
    {
        Task.Run(async () =>
        {
            ApiResponse response = await SteamApiWrapper.GetAppNameAsync(ConnectionTestAppId);
            ApiResponse = response.ResponseState == ResponseState.Success ? "Connected." : "No connection.";
        });
    }

    private void RetryAutoResolve(object sender, RoutedEventArgs e)
    {
        Conductor.RetryAutoResolve();
    }

    // manual resolve would require all apps to have failed;  
    //  but name resolution is cancelled early on network failure  
    //  meaning some apps might neither be resolved nor unresolved 
    // this could be fixed by simply failing all apps on cancellation
    
    // private void ResolveManually(object sender, RoutedEventArgs e)
    // {
    //     Conductor.ResolveFailuresManually();
    // }
}