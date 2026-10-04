using Chatur.Core.Actions;
using Chatur.Core.Prerequisites;
using Xunit;

namespace Chatur.Tests;

/// <summary>Tests for <see cref="NightlyRelease"/> (REQ-FN-013).</summary>
public sealed class NightlyReleaseTests
{
    /// <summary>When a tag carries a "v" prefix, then the version parses without it.</summary>
    [Fact]
    public void ParseVersionReadsAVPrefixedTag()
    {
        var vVersion = NightlyRelease.ParseVersion("v0.1.1");

        Assert.Equal(new Version(0, 1, 1), vVersion);
    }

    /// <summary>When a tag carries no digits at all, then no version is read.</summary>
    [Fact]
    public void ParseVersionReturnsNullForATagWithNoVersion()
    {
        var vVersion = NightlyRelease.ParseVersion("nightly");

        Assert.Null(vVersion);
    }

    /// <summary>When the commitish is a full 40-character SHA, then only the first 7 characters are kept.</summary>
    [Fact]
    public void ShortCommitTrimsAFullSha()
    {
        var vShort = NightlyRelease.ShortCommit("4c7e9b0aa11122233344455566677788899aabb");

        Assert.Equal("4c7e9b0", vShort);
    }

    /// <summary>When the commitish is a branch name rather than a SHA, then it is left unchanged.</summary>
    [Fact]
    public void ShortCommitLeavesABranchNameAlone()
    {
        var vShort = NightlyRelease.ShortCommit("main");

        Assert.Equal("main", vShort);
    }

    /// <summary>When the commitish is absent, then an empty string comes back rather than a null reference.</summary>
    [Fact]
    public void ShortCommitTreatsANullCommitishAsEmpty()
    {
        var vShort = NightlyRelease.ShortCommit(null);

        Assert.Equal(string.Empty, vShort);
    }

    /// <summary>When one asset name contains "mac" and the machine is a Mac, then that asset's URL is picked.</summary>
    [Fact]
    public void DownloadUrlForThisMachinePicksTheMacAsset()
    {
        var vAssets = new[]
        {
            new GitHubReleaseAsset("Chatur-mac.zip", "https://example.test/mac.zip"),
            new GitHubReleaseAsset("Chatur-win.zip", "https://example.test/win.zip"),
        };

        var vUrl = NightlyRelease.DownloadUrlForThisMachine(vAssets, aIsMacOs: true);

        Assert.Equal("https://example.test/mac.zip", vUrl);
    }

    /// <summary>When no asset name matches the machine, then no URL is offered rather than a wrong one.</summary>
    [Fact]
    public void DownloadUrlForThisMachineReturnsNullWhenNothingMatches()
    {
        var vAssets = new[] { new GitHubReleaseAsset("readme.txt", "https://example.test/readme.txt") };

        var vUrl = NightlyRelease.DownloadUrlForThisMachine(vAssets, aIsMacOs: true);

        Assert.Null(vUrl);
    }

    /// <summary>When the release's version is higher than the running build's, then it is offered as an update.</summary>
    [Fact]
    public void EvaluateOfferOffersAHigherVersion()
    {
        var vCandidate = new GitHubRelease("v0.2.0", "main", DateTime.UtcNow, Array.Empty<GitHubReleaseAsset>());
        var vOwn = new ChaturBuildInfo("0.1.0", "aaaaaaa", DateTime.UtcNow.AddDays(-1));

        var vOffer = NightlyRelease.EvaluateOffer(vCandidate, vOwn, aIsMacOs: false);

        Assert.NotNull(vOffer);
        Assert.Equal("0.2.0", vOffer!.Version);
    }

    /// <summary>
    /// When the release matches the running build's own version and commit, then nothing is offered —
    /// this Chatur already is that build.
    /// </summary>
    [Fact]
    public void EvaluateOfferOffersNothingForTheSameBuild()
    {
        var vCandidate = new GitHubRelease("v0.1.0", "aaaaaaaaaa", DateTime.UtcNow, Array.Empty<GitHubReleaseAsset>());
        var vOwn = new ChaturBuildInfo("0.1.0", "aaaaaaa", DateTime.UtcNow.AddHours(-1));

        var vOffer = NightlyRelease.EvaluateOffer(vCandidate, vOwn, aIsMacOs: false);

        Assert.Null(vOffer);
    }

    /// <summary>
    /// When a same-version nightly carries a different commit published after this build, then it is
    /// offered — two nightlies can share a version number and still differ (Directory.Build.props
    /// keeps <c>VersionPrefix</c> fixed between tagged releases).
    /// </summary>
    [Fact]
    public void EvaluateOfferOffersASameVersionNewerCommit()
    {
        var vBuiltUtc = DateTime.UtcNow.AddHours(-2);
        var vCandidate = new GitHubRelease("v0.1.0", "bbbbbbbbbb", DateTime.UtcNow, Array.Empty<GitHubReleaseAsset>());
        var vOwn = new ChaturBuildInfo("0.1.0", "aaaaaaa", vBuiltUtc);

        var vOffer = NightlyRelease.EvaluateOffer(vCandidate, vOwn, aIsMacOs: false);

        Assert.NotNull(vOffer);
        Assert.Equal("bbbbbbb", vOffer!.Commit);
    }

    /// <summary>
    /// When a same-version release carries a different commit but was published before this build,
    /// then it is not offered — a stale or out-of-order release is never shown as an update.
    /// </summary>
    [Fact]
    public void EvaluateOfferIgnoresASameVersionOlderCommit()
    {
        var vCandidate = new GitHubRelease("v0.1.0", "bbbbbbbbbb", DateTime.UtcNow.AddDays(-5), Array.Empty<GitHubReleaseAsset>());
        var vOwn = new ChaturBuildInfo("0.1.0", "aaaaaaa", DateTime.UtcNow);

        var vOffer = NightlyRelease.EvaluateOffer(vCandidate, vOwn, aIsMacOs: false);

        Assert.Null(vOffer);
    }
}
