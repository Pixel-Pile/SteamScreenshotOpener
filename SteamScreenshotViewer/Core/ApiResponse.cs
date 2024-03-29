using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Core;

public enum ResponseState
{
    InvalidEnumValue = 0,
    Success,
    FailureSkipApp,
    FailureRetryApp,
    CancelAll
}

public class ApiResponse
{
    private static ApiResponse _retryReponse = new(ResponseState.FailureRetryApp, null, null);

    private static ApiResponse _cancelAllResponse = new(ResponseState.CancelAll, null, Model.FailureCause.Network);


    private ApiResponse(ResponseState responseState, string? name, FailureCause? failureCause)
    {
        Name = name;
        FailureCause = failureCause;
        ResponseState = responseState;
    }

    public static ApiResponse Success(string name)
    {
        return new ApiResponse(ResponseState.Success, name, null);
    }

    public static ApiResponse RetryApp()
    {
        return _retryReponse;
    }

    public static ApiResponse SkipApp(FailureCause failureCause)
    {
        return new ApiResponse(ResponseState.FailureSkipApp, null, failureCause);
    }

    public static ApiResponse CancelAll()
    {
        return _cancelAllResponse;
    }

    public ResponseState ResponseState { get; }
    public FailureCause? FailureCause { get; }
    public string? Name { get; }
}