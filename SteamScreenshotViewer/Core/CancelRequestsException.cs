namespace SteamScreenshotViewer.Core;

public class CancelRequestsException : Exception
{
    public CancelRequestsException()
    {
    }

    public CancelRequestsException(string message) : base(message)
    {
    }

    public CancelRequestsException(string message, Exception inner) : base(message, inner)
    {
    }
}