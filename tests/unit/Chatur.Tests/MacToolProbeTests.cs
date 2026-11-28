using Chatur.Core.Actions;
using Chatur.Core.Prerequisites;
using Xunit;

namespace Chatur.Tests;

/// <summary>Tests for the Mac-only Xcode row of <see cref="ToolProbeCatalog"/> (27 Nov 2026, the first Mac walk).</summary>
public sealed class MacToolProbeTests
{
    private static readonly ProjectToolSignals MauiProject = new(HasDotNetProject: true, UsesMaui: true, HasNodePackage: false, HasDocker: false);

    /// <summary>
    /// When Chatur runs on a Mac and the selected project is a MAUI project, then Prerequisites lists Xcode after the
    /// MAUI workload, because the Mac Catalyst and iOS heads build with it.
    /// </summary>
    [Fact]
    public void MauiProjectOnAMacNeedsXcode()
    {
        var vNames = ToolProbeCatalog.ForProject(MauiProject, aIsMac: true).Select(aTool => aTool.ToolName);

        Assert.Equal(new[] { "git", ".NET SDK", "MAUI workload", "Xcode" }, vNames);
    }

    /// <summary>
    /// When Chatur runs on Windows, then a MAUI project's list is exactly what it was before the Mac walk: no Xcode.
    /// </summary>
    [Fact]
    public void MauiProjectOffAMacHasNoXcode()
    {
        var vNames = ToolProbeCatalog.ForProject(MauiProject, aIsMac: false).Select(aTool => aTool.ToolName);

        Assert.Equal(new[] { "git", ".NET SDK", "MAUI workload" }, vNames);
    }

    /// <summary>When a project does not use MAUI, then even on a Mac Xcode is not asked for.</summary>
    [Fact]
    public void PlainDotNetProjectOnAMacHasNoXcode()
    {
        var vSignals = MauiProject with { UsesMaui = false };

        Assert.DoesNotContain(ToolProbeCatalog.ForProject(vSignals, aIsMac: true), aTool => aTool.ToolName == "Xcode");
    }

    /// <summary>
    /// When xcodebuild answers with Xcode's version, then the version is read; when it prints nothing on standard
    /// output (Apple's stub on a Mac with no Xcode), then nothing is found and the row offers its fix.
    /// </summary>
    [Fact]
    public void XcodeVersionIsReadFromXcodebuild()
    {
        Assert.Equal("27.2", ToolProbeCatalog.Xcode.ParseVersion("Xcode 27.2\nBuild version 27B52\n"));
        Assert.Null(ToolProbeCatalog.Xcode.ParseVersion(string.Empty));
        Assert.Contains("macappstore://", ToolProbeCatalog.Xcode.MacFixCommand, StringComparison.Ordinal);
    }
}
