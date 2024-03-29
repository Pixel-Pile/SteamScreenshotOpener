using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Core;
using SteamScreenshotViewer.Helper;
using SteamScreenshotViewer.Model;
using GameResolver = SteamScreenshotViewer.Core.GameResolver;

namespace SteamScreenshotViewer.Views;

public partial class ViewApps : TopLevelView
{
    private static ILogger log = Log.ForContext<ViewApps>();

    [ObservableProperty] private Conductor conductor;
    [ObservableProperty] private string searchString;

    private ICollectionView collectionView;

    public ViewApps(Conductor conductor)
    {
        Conductor = conductor;
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
        collectionView = CollectionViewSource.GetDefaultView(Conductor.ObservedResolvedApps);
        collectionView.SortDescriptions.Add(new SortDescription(nameof(ResolvedSteamApp.Name),
            ListSortDirection.Ascending));
    }


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

    private void OpenScreenShotFolder(ResolvedSteamApp app)
    {
        ExplorerHelper.OpenExplorerAtPath(app.ScreenshotsPath);
    }

    private void OnAppClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: ResolvedSteamApp steamApp })
        {
            OpenScreenShotFolder(steamApp);
        }
    }

    private void TextBoxOnKeyDownHandler(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
            {
                HandleArrowKeyDownOnTextBox(e);
                break;
            }
            case Key.Return:
            {
                HandleEnterOnTextBox();
                break;
            }
        }
    }

    private void HandleEnterOnTextBox()
    {
        // get first element
        DependencyObject item = ListViewApps.ItemContainerGenerator.ContainerFromIndex(0);
        if (item is null)
        {
            log.Warning(
                "User pressed enter in apps view but ListViewApps.ItemContainerGenerator.ContainerFromIndex(0) was null. Maybe no app contains search string.");
            return;
        }

        if (item is ListViewItem { DataContext: ResolvedSteamApp app })
        {
            OpenScreenShotFolder(app);
        }
    }

    private void HandleArrowKeyDownOnTextBox(KeyEventArgs e)
    {
        // select first element
        DependencyObject? obj = ListViewApps.ItemContainerGenerator.ContainerFromIndex(0);
        if (obj is ListViewItem item)
        {
            item.Focus();
            e.Handled = true;
        }
        else
        {
            log.Warning("ListViewApps.ItemContainerGenerator.ContainerFromIndex returned object of type " +
                        obj?.GetType());
        }
    }


    private void ItemKeyDownHandler(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
            {
                HandleArrowKeyUpOnListView(sender, e);
                break;
            }
            case Key.Enter:
            {
                HandleEnterOnListView(sender);

                break;
            }
        }
    }

    private void HandleEnterOnListView(object sender)
    {
        if (sender is ListView listView)
        {
            object item = listView.SelectedItem;
            if (item is ResolvedSteamApp app)
                OpenScreenShotFolder(app);
            else
            {
                log.Warning($"listView.SelectedItem had type '{item.GetType().Name}'");
            }
        }
    }

    private void HandleArrowKeyUpOnListView(object sender, KeyEventArgs e)
    {
        if (sender is ListView lv)
        {
            if (lv.SelectedIndex == 0)
            {
                TextBoxSearch.Focus();
                e.Handled = true;
            }
        }
    }

    private void OpenHyperlink(object sender, RequestNavigateEventArgs e)
    {
        HyperlinkHelper.OpenHyperlink(e);
    }
}