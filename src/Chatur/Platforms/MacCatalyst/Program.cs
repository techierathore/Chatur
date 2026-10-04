using UIKit;

namespace Chatur;

/// <summary>The Mac Catalyst entry point.</summary>
public static class Program
{
    /// <summary>
    /// Starts the UIKit application with <see cref="AppDelegate"/>.
    /// </summary>
    /// <param name="aArgs">The process arguments, passed straight to UIKit.</param>
    private static void Main(string[] aArgs)
    {
        UIApplication.Main(aArgs, null, typeof(AppDelegate));
    }
}
