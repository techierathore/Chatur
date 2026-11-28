namespace Chatur.Core.Platform;

/// <summary>
/// Which kind of machine Chatur runs on. Inside the Mac app (Mac Catalyst) <see cref="OperatingSystem.IsMacOS"/> is
/// false and only <see cref="OperatingSystem.IsMacCatalyst"/> is true, while the unit tests and the web harness run as
/// plain macOS; every Mac rule asks <see cref="IsMac"/> so it holds in both (27 Nov 2026, the first Mac install).
/// </summary>
public static class HostPlatform
{
    /// <summary>Whether this process runs on a Mac, as the Mac app or as a plain macOS process.</summary>
    public static bool IsMac => IsMacFor(OperatingSystem.IsMacOS(), OperatingSystem.IsMacCatalyst());

    /// <summary>Whether the file system compares names without regard to case: Windows and a Mac.</summary>
    public static bool HasCaseInsensitiveNames => OperatingSystem.IsWindows() || IsMac;

    /// <summary>The rule behind <see cref="IsMac"/>, taking both answers so either one can be tested anywhere.</summary>
    /// <param name="aIsMacOs">What <see cref="OperatingSystem.IsMacOS"/> answered.</param>
    /// <param name="aIsMacCatalyst">What <see cref="OperatingSystem.IsMacCatalyst"/> answered.</param>
    /// <returns>True when either is true.</returns>
    public static bool IsMacFor(bool aIsMacOs, bool aIsMacCatalyst) => aIsMacOs || aIsMacCatalyst;
}
