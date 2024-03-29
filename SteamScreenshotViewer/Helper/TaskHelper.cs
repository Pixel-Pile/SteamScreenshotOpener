using Serilog;

namespace SteamScreenshotViewer.Helper;

public static class TaskHelper
{
    private static ILogger log = Log.ForContext(typeof(TaskHelper));
    public static event EventHandler<UnobservedTaskExceptionEventArgs>? UnobservedTaskException;

    public static void Run(Action action)
    {
        Task.Run(action).ContinueWith(HandleExceptions);
    }

    public static void Run(Func<Task> action)
    {
        Task.Run(action).ContinueWith(HandleExceptions, TaskContinuationOptions.OnlyOnFaulted);
    }

    private static void HandleExceptions(Task completedTask)
    {
        log.Warning("a task faulted due to " + (completedTask.Exception!.GetType()));
        NonNull.InvokeEvent(UnobservedTaskException,
            new UnobservedTaskExceptionEventArgs(completedTask.Exception));
    }
}