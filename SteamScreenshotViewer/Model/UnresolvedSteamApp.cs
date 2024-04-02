using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamScreenshotViewer.Core;
using SteamScreenshotViewer.Helper;
using GameResolver = SteamScreenshotViewer.Core.GameResolver;

namespace SteamScreenshotViewer.Model;

public enum FailureCause
{
    InvalidEnumValue = 0,
    Network,
    SteamApi,
    DuplicateName
}

[INotifyPropertyChanged]
public partial class UnresolvedSteamApp : SteamAppExtension
{
    private readonly GameResolver _resolver;

    public string DescriptionSteamDb => "Search ID on SteamDB";
    public string DescriptionRetrySteamApi => "Retry Resolving with SteamAPI";
    public string DescriptionOpenFolder => "Open Screenshot Folder";

    public UnresolvedSteamApp(ISteamApp app, FailureCause failureCause, GameResolver resolver) : base(app)
    {
        _resolver = resolver;
        FailureCause = failureCause;
        inConstructor = false;
    }

    public UnresolvedSteamApp(ResolvedSteamApp app, FailureCause failureCause, GameResolver resolver) : base(app)
    {
        _resolver = resolver;
        NameCandidate = app.Name;
        FailureCause = failureCause;
        inConstructor = false;
    }

    [ObservableProperty] private FailureCause failureCause;
    [ObservableProperty] private string nameCandidate = string.Empty;
    [ObservableProperty] private string cleanedNameCandidate = string.Empty;
    [ObservableProperty] private bool? nameCandidateValid;
    [ObservableProperty] private bool retrySteamApiCommandEnabled;
    private readonly bool inConstructor;

    partial void OnNameCandidateChanged(string? oldValue, string newValue)
    {
        CleanedNameCandidate = StringHelper.RemoveDuplicateWhitespace(newValue);
    }

    partial void OnCleanedNameCandidateChanged(string? oldValue, string newValue)
    {
        _resolver.ValidateNameCandidate(this, oldValue);
    }


    partial void OnFailureCauseChanged(FailureCause value)
    {
        switch (value)
        {
            case FailureCause.Network:
                RetrySteamApiCommandEnabled = true;
                break;

            default:
                RetrySteamApiCommandEnabled = false;
                break;
        }
    }


    [RelayCommand]
    private Task OpenScreenshotFolder()
    {
        ExplorerHelper.OpenExplorerAtPath(ScreenshotsPath);
        return Task.CompletedTask;
    }


    [RelayCommand]
    private Task SearchOnSteamDb()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"https://steamdb.info/app/{Id}/",
            UseShellExecute = true
        });
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(RetrySteamApiCommandEnabled))]
    private async Task RetrySteamApi()
    {
        //FIXME
        ApiResponse response = await SteamApiWrapper.GetAppNameAsync(Id);
        if (response.ResponseState == ResponseState.Success)
        {
            NameCandidate = response.Name!;
        }

        RetrySteamApiCommandEnabled = false;
    }
}