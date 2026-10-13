using Chatur.Core.Platform;

namespace Chatur.Platform;

/// <summary>
/// <see cref="IAppShell"/> for the MAUI heads: Exit quits the application, and Zoom is
/// <see cref="BlazorZoom"/>, the same zoom the keyboard shortcuts drive (REQ-UI-073). Each call runs
/// on the UI thread whichever thread the menu click arrived on.
/// </summary>
public sealed class MauiAppShell : IAppShell
{
    /// <inheritdoc />
    public void Exit() => MainThread.BeginInvokeOnMainThread(() => Application.Current?.Quit());

    /// <inheritdoc />
    public void ZoomBy(int aSteps) => MainThread.BeginInvokeOnMainThread(() => BlazorZoom.ZoomBy(aSteps));

    /// <inheritdoc />
    public void ResetZoom() => MainThread.BeginInvokeOnMainThread(BlazorZoom.Reset);
}
