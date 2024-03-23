using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SteamScreenshotViewer.Views;

[INotifyPropertyChanged]
public partial class ViewBasePathDialog : UserControl
{
    public ViewBasePathDialog()
    {
        SubmitButtonCommandInternal = new RelayCommand<string>(HandleSubmitCommand);
        InitializeComponent();
    }

    public static readonly DependencyProperty SubmitButtonCommandInternalProperty = DependencyProperty.Register(
        nameof(SubmitButtonCommandInternal), typeof(RelayCommand<string>), typeof(ViewBasePathDialog),
        new PropertyMetadata(default(RelayCommand<string>)));

    public RelayCommand<string> SubmitButtonCommandInternal
    {
        get { return (RelayCommand<string>)GetValue(SubmitButtonCommandInternalProperty); }
        set { SetValue(SubmitButtonCommandInternalProperty, value); }
    }

    public static readonly DependencyProperty SubmitButtonCommandProperty = DependencyProperty.Register(
        nameof(SubmitButtonCommand), typeof(RelayCommand<string>), typeof(ViewBasePathDialog),
        new PropertyMetadata(default(RelayCommand<string>)));

    public RelayCommand<string> SubmitButtonCommand
    {
        get { return (RelayCommand<string>)GetValue(SubmitButtonCommandProperty); }
        set { SetValue(SubmitButtonCommandProperty, value); }
    }

    [ObservableProperty] private string errorMessage;

    private void HandleSubmitCommand(string gameSpecificScreenshotPath)
    {
        if (string.IsNullOrEmpty(gameSpecificScreenshotPath))
        {
            ErrorMessage = "base path cannot be empty";
            return;
        }

        if (!System.IO.Path.Exists(gameSpecificScreenshotPath))
        {
            ErrorMessage = "path does not exist: " + gameSpecificScreenshotPath;
            return;
        }

        SubmitButtonCommand.Execute(gameSpecificScreenshotPath);
    }
}