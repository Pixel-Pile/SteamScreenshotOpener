using SteamScreenshotViewer.Model;

namespace SteamScreenshotViewer.Helper;

public class ApiResponse
{
    private static ApiResponse _retryReponse = new ApiResponse(false, null, null, true);

    private ApiResponse(bool containsName, string? name, FailureCause? failureCause, bool shouldRetry)
    {
        ContainsName = containsName;
        Name = name;
        FailureCause = failureCause;
        ShouldRetry = shouldRetry;
    }

    public static ApiResponse Success(string name)
    {
        return new ApiResponse(true, name, null, false);
    }

    public static ApiResponse Failure(FailureCause failureCause)
    {
        return new ApiResponse(false, null, failureCause, false);
    }

    public static ApiResponse Retry()
    {
        return _retryReponse;
    }

    public bool ContainsName { get; }
    public string? Name { get; }
    public FailureCause? FailureCause { get; }

    public bool ShouldRetry { get; }
}