using System.Reflection;
using Chatur.Core.Prerequisites;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Proves REQ-NFR-006: the release workflow builds a Mac Catalyst zip and an unpackaged,
/// self-contained Windows zip, publishes the nightly pre-release on a push to main and a named
/// release on a <c>v*</c> tag, and stamps version and commit into the build so Prerequisites and the
/// newer-build check can read them. The workflow itself only runs on GitHub, so these tests read the
/// workflow file and the stamped assembly and prove the pieces fit together.
/// </summary>
public sealed class ReleasePipelineTests
{
    private static string RepoRoot()
    {
        var vDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (vDirectory is not null && !File.Exists(Path.Combine(vDirectory.FullName, "Chatur.sln")))
        {
            vDirectory = vDirectory.Parent;
        }

        Assert.NotNull(vDirectory);
        return vDirectory!.FullName;
    }

    private static string Workflow() =>
        File.ReadAllText(Path.Combine(RepoRoot(), ".github", "workflows", "release.yml"));

    /// <summary>
    /// When the workflow is read, then it has a macOS job that publishes the unsigned Mac Catalyst
    /// app and zips the <c>.app</c>.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-006 the workflow has a Mac job that zips the app")]
    public void WorkflowHasAMacJobThatZipsTheApp()
    {
        var vText = Workflow();

        Assert.Contains("runs-on: macos-latest", vText);
        Assert.Contains("-f net10.0-maccatalyst", vText);
        Assert.Contains("-maccatalyst.zip", vText);
    }

    /// <summary>
    /// When the workflow is read, then it has a Windows job that publishes the unpackaged,
    /// self-contained app and zips it.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-006 the workflow has a Windows job that zips an unpackaged app")]
    public void WorkflowHasAWindowsJobThatZipsAnUnpackagedApp()
    {
        var vText = Workflow();

        Assert.Contains("runs-on: windows-latest", vText);
        Assert.Contains("-p:WindowsPackageType=None", vText);
        Assert.Contains("--self-contained", vText);
        Assert.Contains("-win-x64.zip", vText);
    }

    /// <summary>
    /// When the workflow is read, then a push to main publishes a pre-release holding both zips and a
    /// <c>v*</c> tag publishes a named release.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-006 the workflow publishes nightly on main and a named release on a tag")]
    public void WorkflowPublishesNightlyOnMainAndANamedReleaseOnATag()
    {
        var vText = Workflow();

        Assert.Contains("branches: [main]", vText);
        Assert.Contains("tags: ['v*'", vText);
        Assert.Contains("--prerelease", vText);
        Assert.Contains("dist/*.zip", vText);
        Assert.Contains("--target \"${GITHUB_SHA}\"", vText);
        Assert.Contains("gh release create \"$tag\"", vText);
    }

    /// <summary>When the workflow is read, then it signs nothing (added 2026-09-30).</summary>
    [Fact(DisplayName = "REQ-NFR-006 the workflow does no code signing")]
    public void WorkflowDoesNoCodeSigning()
    {
        var vText = Workflow();

        Assert.Contains("-p:EnableCodeSigning=false", vText);
        Assert.DoesNotContain("codesign ", vText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("signtool", vText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// When the workflow's own asset names are fed to the newer-build check's asset picker, then a Mac
    /// gets the Mac zip and any other machine gets the Windows zip.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-006 the workflow asset names are found by the newer-build check")]
    public void WorkflowAssetNamesAreFoundByTheNewerBuildCheck()
    {
        var vAssets = new List<GitHubReleaseAsset>
        {
            new("Chatur-0.1.0-nightly-maccatalyst.zip", "mac-url"),
            new("Chatur-0.1.0-nightly-win-x64.zip", "win-url"),
        };

        Assert.Equal("mac-url", NightlyRelease.DownloadUrlForThisMachine(vAssets, true));
        Assert.Equal("win-url", NightlyRelease.DownloadUrlForThisMachine(vAssets, false));
    }

    /// <summary>
    /// When the build is stamped, then the informational version carries the same commit that the
    /// <c>Chatur.CommitSha</c> metadata holds, which is what Prerequisites shows.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-006 the stamped version and commit agree")]
    public void StampedVersionAndCommitAgree()
    {
        var vAssembly = typeof(NightlyRelease).Assembly;
        var vInformational = vAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var vCommit = vAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(aEntry => aEntry.Key == "Chatur.CommitSha").Value;

        var vPlus = vInformational.IndexOf('+');
        Assert.True(vPlus > 0, vInformational);
        Assert.False(string.IsNullOrWhiteSpace(vCommit));
        Assert.Equal(vCommit, vInformational[(vPlus + 1)..]);
        Assert.NotNull(NightlyRelease.ParseVersion("v" + vInformational[..vPlus]));
    }

    /// <summary>
    /// When the workflow passes the commit it was triggered for, then that value reaches the build as
    /// the commit to stamp.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-006 the workflow stamps the triggering commit")]
    public void WorkflowStampsTheTriggeringCommit()
    {
        Assert.Contains("-p:ChaturCommitSha=${GITHUB_SHA}", Workflow());
    }
}
