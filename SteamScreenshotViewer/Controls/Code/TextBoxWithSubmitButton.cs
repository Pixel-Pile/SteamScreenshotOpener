﻿using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;

namespace SteamScreenshotViewer.Controls.Code;

public class TextBoxWithSubmitButton : Control
{
    static TextBoxWithSubmitButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBoxWithSubmitButton),
            new FrameworkPropertyMetadata(typeof(TextBoxWithSubmitButton)));
    }

    public TextBoxWithSubmitButton()
    {
        SubmitButtonCommandInternal = new RelayCommand(ExecuteCommandWithTextBoxText);
        SubmitButtonCommand = new RelayCommand<string>(s => { });
    }


    private void ExecuteCommandWithTextBoxText()
    {
        SubmitButtonCommand.Execute(TextBox.Text);
    }

    public static readonly DependencyProperty SubmitButtonCommandInternalProperty = DependencyProperty.Register(
        nameof(SubmitButtonCommandInternal), typeof(RelayCommand), typeof(TextBoxWithSubmitButton),
        new PropertyMetadata(default(RelayCommand)));

    public RelayCommand SubmitButtonCommandInternal
    {
        get { return (RelayCommand)GetValue(SubmitButtonCommandInternalProperty); }
        set { SetValue(SubmitButtonCommandInternalProperty, value); }
    }


    public static readonly DependencyProperty SubmitButtonCommandProperty = DependencyProperty.Register(
        nameof(SubmitButtonCommand), typeof(RelayCommand<string>), typeof(TextBoxWithSubmitButton),
        new PropertyMetadata(default(RelayCommand<string>)));


    public RelayCommand<string> SubmitButtonCommand
    {
        get { return (RelayCommand<string>)GetValue(SubmitButtonCommandProperty); }
        set { SetValue(SubmitButtonCommandProperty, value); }
    }


    public static readonly DependencyProperty PromptProperty = DependencyProperty.Register(
        nameof(Prompt), typeof(TextBlock), typeof(TextBoxWithSubmitButton), new PropertyMetadata(default(TextBlock)));

    public TextBlock Prompt
    {
        get { return (TextBlock)GetValue(PromptProperty); }
        set { SetValue(PromptProperty, value); }
    }


    public static readonly DependencyProperty TextBoxProperty = DependencyProperty.Register(
        nameof(TextBox), typeof(TextBox), typeof(TextBoxWithSubmitButton), new PropertyMetadata(default(TextBox)));

    public TextBox TextBox
    {
        get { return (TextBox)GetValue(TextBoxProperty); }
        set { SetValue(TextBoxProperty, value); }
    }

    public static readonly DependencyProperty SubmitButtonProperty = DependencyProperty.Register(
        nameof(SubmitButton), typeof(Button), typeof(TextBoxWithSubmitButton), new PropertyMetadata(default(Button)));

    public Button SubmitButton
    {
        get { return (Button)GetValue(SubmitButtonProperty); }
        set { SetValue(SubmitButtonProperty, value); }
    }
}