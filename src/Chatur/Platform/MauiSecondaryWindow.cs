using Chatur.Core.Platform;
using Chatur.Services;

namespace Chatur.Platform;

/// <summary>
/// <see cref="ISecondaryWindow"/> over <see cref="IWindowService"/>: a menu item in the main window
/// opens the real secondary window, on the UI thread whichever thread the click arrived on
/// (REQ-FN-106).
/// </summary>
public sealed class MauiSecondaryWindow : ISecondaryWindow
{
    private readonly IWindowService objWindows;

    /// <summary>Creates the port.</summary>
    /// <param name="aWindows">The service that owns Chatur's windows.</param>
    public MauiSecondaryWindow(IWindowService aWindows)
    {
        objWindows = aWindows;
    }

    /// <inheritdoc />
    public void Open(string aRoute) =>
        MainThread.BeginInvokeOnMainThread(() => objWindows.ShowSecondaryWindow(aRoute));

    /// <inheritdoc />
    public void Close() =>
        MainThread.BeginInvokeOnMainThread(objWindows.CloseSecondaryWindow);
}
