using System.Diagnostics;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SteamScreenshotViewer.Model;

public enum FailureCause
{
    Error,
    Network,
    SteamApi,
    DuplicateName
}

[INotifyPropertyChanged]
public partial class UnresolvedSteamApp : SteamAppExtension
{
    private readonly GameResolver _resolver;


    public const string ConstDescriptionSteamDb = "Search ID on SteamDB";
    public const string ConstDescriptionRetrySteamApi = "Retry Resolving with SteamAPI";
    public const string ConstDescriptionOpenFolder = "Open Screenshot Folder";

    public string DescriptionSteamDb => ConstDescriptionSteamDb;
    public string DescriptionRetrySteamApi => ConstDescriptionRetrySteamApi;
    public string DescriptionOpenFolder => ConstDescriptionOpenFolder;

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
    [ObservableProperty] private String nameCandidate = string.Empty;
    [ObservableProperty] private bool? nameCandidateValid;
    [ObservableProperty] private bool retrySteamApiCommandEnabled;
    private readonly bool inConstructor;

    partial void OnNameCandidateChanged(string? oldValue, string newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        _resolver.ValidateNameCandidate(this);
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
        Process.Start("explorer.exe", this.ScreenshotsPath);
        return Task.CompletedTask;
    }


    [RelayCommand]
    private Task SearchOnSteamDb()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"https://steamdb.info/app/{this.Id}/",
            UseShellExecute = true
        });
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(RetrySteamApiCommandEnabled))]
    private async Task RetrySteamApi()
    {
        ApiResponse response = await SteamApiClient.GetAppNameAsync(this.Id);
        if (response.ContainsName)
        {
            NameCandidate = response.Name!;
        }

        RetrySteamApiCommandEnabled = false;
    }
}