using Chatur.Core.Platform;
using Chatur.Core.Prerequisites;
using Xunit;

namespace Chatur.Tests;

/// <summary>Tests for the Mac app's own <c>PATH</c> (<see cref="HomebrewPath.ForMacApp"/>) and <see cref="HostPlatform"/>.</summary>
public sealed class MacAppPathTests
{
    /// <summary>
    /// When the Mac app is started from Finder with launchd's short PATH, then the folders of /etc/paths and
    /// /etc/paths.d follow it in order, a "~" line names the home folder, and Homebrew and /usr/local/share/dotnet
    /// are added, so dotnet is found.
    /// </summary>
    [Fact]
    public void MacAppPathAddsPathFilesHomebrewAndDotNet()
    {
        var vLines = new[] { "/usr/local/bin", "/usr/bin", "# a remark", "", "/usr/local/share/dotnet", "~/.dotnet/tools" };

        var vEntries = HomebrewPath.ForMacApp("/usr/bin:/bin:/usr/sbin:/sbin", vLines, "/Users/someone").Split(':');

        Assert.Equal(
            new[] { "/usr/bin", "/bin", "/usr/sbin", "/sbin", "/usr/local/bin", "/usr/local/share/dotnet", "/Users/someone/.dotnet/tools", "/opt/homebrew/bin" },
            vEntries);
    }

    /// <summary>
    /// When no path file can be read and PATH is empty, then Homebrew and the .NET install folder are still there.
    /// </summary>
    [Fact]
    public void MacAppPathWithoutPathFiles()
    {
        var vEntries = HomebrewPath.ForMacApp(null, Array.Empty<string>(), "/Users/someone").Split(':');

        Assert.Equal(new[] { "/opt/homebrew/bin", "/usr/local/bin", "/usr/local/share/dotnet" }, vEntries);
    }

    /// <summary>
    /// When the process is the Mac app (Mac Catalyst answers true and macOS false), or a plain macOS process, then it
    /// counts as a Mac; when neither answers true, it does not.
    /// </summary>
    [Fact]
    public void MacCatalystCountsAsMac()
    {
        Assert.True(HostPlatform.IsMacFor(aIsMacOs: false, aIsMacCatalyst: true));
        Assert.True(HostPlatform.IsMacFor(aIsMacOs: true, aIsMacCatalyst: false));
        Assert.False(HostPlatform.IsMacFor(aIsMacOs: false, aIsMacCatalyst: false));
    }

    /// <summary>
    /// When this machine's own /etc/paths files are read on a Mac, then the result still begins with the PATH it was
    /// given and names /usr/local/share/dotnet.
    /// </summary>
    [Fact]
    public void ThisMacAppPathKeepsTheGivenPathFirst()
    {
        var vEntries = HomebrewPath.ForThisMacApp("/usr/bin:/bin").Split(':');

        Assert.Equal("/usr/bin", vEntries[0]);
        Assert.Contains("/usr/local/share/dotnet", vEntries);
    }
}
