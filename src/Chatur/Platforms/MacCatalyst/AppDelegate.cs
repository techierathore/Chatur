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
}
