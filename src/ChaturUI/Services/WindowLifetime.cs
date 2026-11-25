namespace ChaturUI.Services;

/// <summary>
/// One window's page, as the window closes (REQ-NFR-011). The desktop host calls <see cref="Close"/> before it closes
/// a window, and <see cref="ChaturRoutes"/> then draws nothing: every component on the page is taken down while the
/// page's web view is still there, so the messages their clean-up sends (script modules released, listeners removed)
/// reach a live page. Taken down after the web view had gone, each such message failed inside the web view's own
/// bridge and left an "Unobserved task exception" in the log (owner's log, 21 Nov 2026).
/// </summary>
public sealed class WindowLifetime
{
    /// <summary>Raised when the window starts to close.</summary>
    public event Action? Closing;

    /// <summary>Whether the window is closing: the page draws nothing more.</summary>
    public bool IsClosing { get; private set; }

    /// <summary>Marks the window as closing and tells the page to take everything down.</summary>
    public void Close()
    {
        if (IsClosing)
        {
            return;
        }

        IsClosing = true;
        Closing?.Invoke();
    }
}
