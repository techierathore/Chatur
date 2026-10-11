using Foundation;

namespace Chatur;

/// <summary>
/// The Mac Catalyst scene delegate named by <c>UIApplicationSceneManifest</c> in Info.plist. Without a
/// scene manifest and this delegate the app quits at its first window on macOS 27.
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
