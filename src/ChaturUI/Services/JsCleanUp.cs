using Microsoft.JSInterop;

namespace ChaturUI.Services;

/// <summary>
/// Clean-up that speaks to the page's script, for a page that may already have gone (REQ-FN-009).
/// </summary>
/// <remarks>
/// In the desktop app a page that is reloaded (Sign out does that) or whose window closes takes its script with it,
/// and a call made to that script afterwards is never answered: it does not fail, it waits. The app clears the old
/// page away before it attaches the new one, so one such wait in a component's or a service's clean-up left the
/// window on "Loading Chatur" for good (miss of 2026-11-16). Clean-up therefore waits a short, fixed time and then
/// goes on: the listeners it would have removed went with the page.
/// </remarks>
public static class JsCleanUp
{
    /// <summary>How long clean-up waits for the page's script to answer before it goes on without it.</summary>
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Runs <paramref name="aCleanUp"/> and returns when it has finished, failed because the page is gone, or
    /// not answered within <see cref="Wait"/>.
    /// </summary>
    /// <param name="aCleanUp">The calls to the page's script that undo what the component or service set up.</param>
    public static async ValueTask RunAsync(Func<ValueTask> aCleanUp)
    {
        ArgumentNullException.ThrowIfNull(aCleanUp);

        try
        {
            await aCleanUp().AsTask().WaitAsync(Wait).ConfigureAwait(false);
        }
        catch (Exception aEx) when (aEx is TimeoutException or JSDisconnectedException or JSException
            or OperationCanceledException or ObjectDisposedException or InvalidOperationException)
        {
            // The page, its window or its connection has gone, and what was to be removed went with it.
        }
    }
}
