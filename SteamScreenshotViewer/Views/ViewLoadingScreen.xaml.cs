﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Views;

public partial class ViewLoadingScreen : TopLevelView
{
    public ViewLoadingScreen()
    {
        InitializeComponent();
    }

    [ObservableProperty] private GameResolver gameResolver;
}