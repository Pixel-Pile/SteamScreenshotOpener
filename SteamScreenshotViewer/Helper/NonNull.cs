using System.Runtime.CompilerServices;

namespace SteamScreenshotViewer.Helper;

public static class NonNull
{
    public static void InvokeEvent(Action? action, [CallerArgumentExpression(nameof(action))] string? name = null)
    {
        if (action is null)
        {
            throw new NullReferenceException($"Attempted to raise event '{name}' but no handlers were subscribed.");
        }

        action.Invoke();
    }

    public static void InvokeEvent<T>(EventHandler<T>? action, T eventArgs,
        [CallerArgumentExpression(nameof(action))] string? eventName = null)
    {
        if (action is null)
        {
            throw new NullReferenceException(
                $"Attempted to raise event '{eventName}' but no handlers were subscribed.");
        }

        action.Invoke(null, eventArgs);
    }
}