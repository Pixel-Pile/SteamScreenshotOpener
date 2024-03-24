using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamScreenshotViewer.Controls.Code;
using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Views;

public partial class ViewBasePathDialog : TopLevelView
{
    public ViewBasePathDialog(GameResolver resolver)
    {
        SubmitButtonCommandInternal = new RelayCommand<string>(HandleSubmitCommand);
        GameResolver = resolver;
        InitializeComponent();
    }

    [ObservableProperty] private GameResolver gameResolver;


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
            ErrorMessage = "Path cannot be empty.";
            return;
        }

        if (!Path.Exists(gameSpecificScreenshotPath))
        {
            ErrorMessage = "Path does not exist.";
            return;
        }

        if (!gameSpecificScreenshotPath.EndsWith(Path.DirectorySeparatorChar + "screenshots"))
        {
            ErrorMessage = "Screenshot path should end in a directory named \"screenshots\".";
            return;
        }

        SubmitButtonCommand.Execute(gameSpecificScreenshotPath);
    }

    private void OnKeyDownHandler(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return)
        {
            return;
        }


        TextBoxWithSubmitButton.ExecuteCommandWithTextBoxText();
    }
}