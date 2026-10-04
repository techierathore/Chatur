using System.Text.RegularExpressions;
using Chatur.Core.Actions;

namespace Chatur.Core.Prerequisites;

/// <summary>
/// Pure helpers that turn a GitHub release into the pieces <c>NewerBuildAsync</c> needs, kept
/// separate from the HTTP call so they can be unit tested without a network (REQ-FN-013).
/// </summary>
public static class NightlyRelease
{
    /// <summary>Reads the version out of a release tag such as <c>"v0.1.1"</c> or <c>"0.1.1"</c>.</summary>
    /// <param name="aTagName">The release's tag name.</param>
    /// <returns>The parsed version, or <see langword="null"/> when the tag carries no version number.</returns>
    public static Version? ParseVersion(string aTagName)
    {
        var vMatch = Regex.Match(aTagName, @"(\d+(?:\.\d+){1,3})");
        return vMatch.Success && Version.TryParse(vMatch.Groups[1].Value, out var vVersion) ? vVersion : null;
    }

    /// <summary>Shortens a full commit SHA to the 7 characters Prerequisites shows.</summary>
    /// <param name="aTargetCommitish">The release's <c>target_commitish</c>: a commit SHA or a branch name.</param>
    /// <returns>The short commit, or the value unchanged when it does not look like a SHA, or <c>""</c> when absent.</returns>
    public static string ShortCommit(string? aTargetCommitish)
    {
        if (string.IsNullOrEmpty(aTargetCommitish))
        {
            return string.Empty;
        }

        return Regex.IsMatch(aTargetCommitish, "^[0-9a-fA-F]{7,40}$")
            ? aTargetCommitish[..7]
            : aTargetCommitish;
    }

    /// <summary>Picks the asset built for the machine Chatur is running on.</summary>
    /// <param name="aAssets">Every asset attached to the release.</param>
    /// <param name="aIsMacOs">Whether the current machine is a Mac.</param>
    /// <returns>The matching asset's download URL, or <see langword="null"/> when none matches.</returns>
    public static string? DownloadUrlForThisMachine(IReadOnlyList<GitHubReleaseAsset> aAssets, bool aIsMacOs)
    {
        var vNeedle = aIsMacOs ? "mac" : "win";
        return aAssets.FirstOrDefault(aA => aA.Name.Contains(vNeedle, StringComparison.OrdinalIgnoreCase))?.DownloadUrl;
    }

    /// <summary>
    /// Decides whether <paramref name="aCandidate"/> is a real update over <paramref name="aOwn"/>,
    /// and builds the <see cref="ChaturBuildInfo"/> Prerequisites offers when it is (REQ-FN-013). Kept
    /// as a pure function of its inputs — no assembly reflection, no network — so it is unit testable
    /// with made-up releases and versions.
    /// </summary>
    /// <param name="aCandidate">The newest release <see cref="IGitHubReleaseClient.LatestReleaseAsync"/> found.</param>
    /// <param name="aOwn">This Chatur's own build identity.</param>
    /// <param name="aIsMacOs">Whether the current machine is a Mac, for picking the right asset.</param>
    /// <returns>The offer to show, or <see langword="null"/> when <paramref name="aCandidate"/> is not newer.</returns>
    public static ChaturBuildInfo? EvaluateOffer(GitHubRelease aCandidate, ChaturBuildInfo aOwn, bool aIsMacOs)
    {
        var vCandidateVersion = ParseVersion(aCandidate.TagName);
        var vOwnVersion = ParseVersion(aOwn.Version);
        var vCandidateCommit = ShortCommit(aCandidate.TargetCommitish);

        var vIsNewerVersion = vCandidateVersion is not null && vOwnVersion is not null && vCandidateVersion > vOwnVersion;

        // VersionPrefix stays fixed across nightlies (Directory.Build.props); two nightlies can share
        // the same Version and differ only by commit and build time, so a same-version release still
        // counts as an update when its commit differs from ours and it was published after we were
        // built — never when it merely looks different due to clock skew or a stale/duplicate release.
        var vIsSameVersionNewerCommit =
            vCandidateVersion is not null && vOwnVersion is not null && vCandidateVersion == vOwnVersion
            && !string.IsNullOrEmpty(vCandidateCommit)
            && !string.Equals(vCandidateCommit, aOwn.Commit, StringComparison.OrdinalIgnoreCase)
            && aCandidate.PublishedUtc is not null && aCandidate.PublishedUtc > aOwn.BuiltUtc;

        if (!vIsNewerVersion && !vIsSameVersionNewerCommit)
        {
            return null;
        }

        return new ChaturBuildInfo(
            vCandidateVersion?.ToString() ?? aOwn.Version,
            vCandidateCommit,
            aCandidate.PublishedUtc ?? DateTime.UtcNow,
            DownloadUrlForThisMachine(aCandidate.Assets, aIsMacOs));
    }
}
