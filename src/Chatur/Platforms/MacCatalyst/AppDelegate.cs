using Foundation;

namespace Chatur;

/// <summary>
/// The Mac Catalyst application delegate the platform head actually starts. Delegates straight to
/// <see cref="MauiProgram.CreateMauiApp"/>, the same as every other platform.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    /// <summary>Adds View › Zoom In, Zoom Out and Actual Size before macOS 26 (§9b; later macOS: <see cref="BlazorZoom.Attach"/>).</summary>
    /// <param name="builder">The main menu builder.</param>
    public override void BuildMenu(UIKit.IUIMenuBuilder builder)
    {
        base.BuildMenu(builder);
        BlazorZoom.BuildMenu(builder);
    }

    /// <summary>View › Zoom In (Cmd+=).</summary>
    /// <param name="aSender">The menu command.</param>
    [Export("tfZoomIn:")]
    public void ZoomIn(NSObject aSender) => BlazorZoom.ZoomBy(1);

    /// <summary>View › Zoom Out (Cmd+-).</summary>
    /// <param name="aSender">The menu command.</param>
    [Export("tfZoomOut:")]
    public void ZoomOut(NSObject aSender) => BlazorZoom.ZoomBy(-1);

    /// <summary>View › Actual Size (Cmd+0).</summary>
    /// <param name="aSender">The menu command.</param>
    [Export("tfZoomReset:")]
    public void ZoomReset(NSObject aSender) => BlazorZoom.Reset();
}
