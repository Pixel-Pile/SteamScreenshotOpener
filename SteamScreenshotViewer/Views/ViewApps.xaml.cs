using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Views;

public partial class ViewApps : TopLevelView
{
    public ViewApps(GameResolver resolver)
    {
        GameResolver = resolver;
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        collectionView = CollectionViewSource.GetDefaultView(GameResolver.ObservedResolvedApps);
        collectionView.SortDescriptions.Add(new SortDescription(nameof(ResolvedSteamApp.Name),
            ListSortDirection.Ascending));
    }

    [ObservableProperty] private GameResolver gameResolver;

    [ObservableProperty] private string searchString;

    private string lowerCaseSearchString;
    private ICollectionView collectionView;


    partial void OnSearchStringChanged(string value)
    {
        // the filter method is invoked for every app in the collection
        // by storing lowerCaseSearchString, searchString.ToLower()
        // only has to be calculated when the string changed
        // instead of for every single item
        lowerCaseSearchString = SearchString.ToLower();
        if (string.IsNullOrEmpty(value))
        {
            collectionView.Filter = null;
            return;
        }

        collectionView.Filter = AppNameContainsSearchString;
    }

    private bool AppNameContainsSearchString(object app)
    {
        return ((ResolvedSteamApp)app).LowerCaseName.Contains(lowerCaseSearchString);
    }

    private void OnAppClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: ResolvedSteamApp steamApp })
        {
            Process.Start("explorer.exe", steamApp.ScreenshotsPath);
        }
    }
}