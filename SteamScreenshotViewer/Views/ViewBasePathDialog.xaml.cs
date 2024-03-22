using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;

namespace SteamScreenshotViewer.Views;

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

    private void HandleSubmitCommand(string gameSpecificScreenshotPath)
    {
        if (string.IsNullOrEmpty(gameSpecificScreenshotPath))
        {
            MessageBox.Show("base path cannot be empty");
            return;
        }

        if (!System.IO.Path.Exists(gameSpecificScreenshotPath))
        {
            MessageBox.Show("path does not exist: " + gameSpecificScreenshotPath);
            return;
        }

        SubmitButtonCommand.Execute(gameSpecificScreenshotPath);
    }
}