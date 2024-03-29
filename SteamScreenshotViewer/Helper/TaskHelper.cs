namespace SteamScreenshotViewer.Helper;

public static class TaskHelper
{
    public static event EventHandler<UnobservedTaskExceptionEventArgs>? UnobservedTaskException;

    public static void Run(Action action)
    {
        Task.Run(action).ContinueWith(HandleExceptions);
    }

    public static void Run(Func<Task> action)
    {
        Task.Run(action).ContinueWith(HandleExceptions);
    }

    private static void HandleExceptions(Task completedTask)
    {
        if (completedTask.IsFaulted)
        {
            NonNull.InvokeEvent(UnobservedTaskException,
                new UnobservedTaskExceptionEventArgs(completedTask.Exception));
        }
    }
}