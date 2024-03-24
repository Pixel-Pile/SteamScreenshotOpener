using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SteamScreenshotViewer.Controls.Code;

[ContentProperty(nameof(View))]
[INotifyPropertyChanged]
public partial class TopLevelView : Control
{
    
    static TopLevelView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TopLevelView),
            new FrameworkPropertyMetadata(typeof(TopLevelView)));
    }
    [ObservableProperty] private string title;
    [ObservableProperty] private TextBlock description;

    [ObservableProperty] private DependencyObject view;
}