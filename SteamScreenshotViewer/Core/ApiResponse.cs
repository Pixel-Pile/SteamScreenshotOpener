using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Core;

public enum ResponseState
{
    InvalidEnumValue = 0,
    Success,
    FailureSkipApp,
    FailureRetryAppWithDifferentFilters,
    CancelAll
}

public class ApiResponse
{
    private class ApiResponseCore
    {
        internal ApiResponseCore(ResponseState responseState, string? name, FailureCause? failureCause)
        {
            Name = name;
            FailureCause = failureCause;
            ResponseState = responseState;
        }

        public ResponseState ResponseState { get; }
        public FailureCause? FailureCause { get; }
        public string? Name { get; }
    }

    private static readonly ApiResponseCore _retryReponse =
        new(ResponseState.FailureRetryAppWithDifferentFilters, null, null);

    private static readonly ApiResponseCore _cancelAllResponse =
        new(ResponseState.CancelAll, null, Model.FailureCause.Network);

    private readonly ApiResponseCore core;

    private ApiResponse(ApiResponseCore core)
    {
        this.core = core;
        TimeStamp = DateTime.Now;
    }


    public static ApiResponse Success(string name)
    {
        return new ApiResponse(new ApiResponseCore(ResponseState.Success, name, null));
    }

    public static ApiResponse RetryAppWithDifferentFilters()
    {
        return new ApiResponse(_retryReponse);
    }

    public static ApiResponse SkipApp(FailureCause failureCause)
    {
        return new ApiResponse(new ApiResponseCore(ResponseState.FailureSkipApp, null, failureCause));
    }

    public static ApiResponse CancelAll()
    {
        return new ApiResponse(_cancelAllResponse);
    }

    public DateTime TimeStamp { get; }
    public ResponseState ResponseState => core.ResponseState;
    public FailureCause? FailureCause => core.FailureCause;
    public string? Name => core.Name;
}