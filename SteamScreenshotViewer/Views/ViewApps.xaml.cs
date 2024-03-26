using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Model;
using GameResolver = SteamScreenshotViewer.Helper.GameResolver;

namespace SteamScreenshotViewer.Views;

public partial class ViewApps : TopLevelView
{
    public ViewApps(GameResolver resolver)
    {
        GameResolver = resolver;
        this.Loaded += OnLoaded;
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TextBoxSearch.Focus();
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

    private ICollectionView collectionView;


    partial void OnSearchStringChanged(string value)
    {
        // the filter method is invoked for every app in the collection
        if (string.IsNullOrEmpty(value))
        {
            collectionView.Filter = null;
            return;
        }

        collectionView.Filter = AppNameContainsSearchString;
    }

    private bool AppNameContainsSearchString(object app)
    {
        return ((ResolvedSteamApp)app).Name.IndexOf(SearchString, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void OnAppClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: ResolvedSteamApp steamApp })
        {
            Process.Start("explorer.exe", steamApp.ScreenshotsPath);
        }
    }
}