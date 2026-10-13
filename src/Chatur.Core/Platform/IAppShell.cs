namespace Chatur.Core.Platform;

/// <summary>
/// Platform port over the running application itself (Architecture §6): quitting it and making
/// everything in the window larger or smaller. The MAUI head quits the app and zooms its web views
/// (<c>BlazorZoom</c>); the web harness stops its server and zooms the browser page (REQ-UI-073).
/// </summary>
public interface IAppShell
{
    /// <summary>Quits Chatur, every window with it.</summary>
    void Exit();

    /// <summary>Makes everything larger (a positive step) or smaller (a negative one), 10% a step, from 50% to 300%.</summary>
    /// <param name="aSteps">How many 10% steps.</param>
    void ZoomBy(int aSteps);

    /// <summary>Back to actual size, 100%.</summary>
    void ResetZoom();
}
