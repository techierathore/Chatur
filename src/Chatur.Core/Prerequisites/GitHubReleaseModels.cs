namespace Chatur.Core.Prerequisites;

/// <summary>One asset attached to a GitHub release — a per-platform zip (REQ-FN-013).</summary>
/// <param name="Name">The asset's file name.</param>
/// <param name="DownloadUrl">Its direct download URL.</param>
public sealed record GitHubReleaseAsset(string Name, string DownloadUrl);

/// <summary>The parts of a GitHub release Doctor reads to decide whether to offer a newer nightly (REQ-FN-013).</summary>
/// <param name="TagName">The release's tag, e.g. <c>"v0.1.1"</c>.</param>
/// <param name="TargetCommitish">The commit or branch the release was cut from.</param>
/// <param name="PublishedUtc">When the release was published.</param>
/// <param name="Assets">The files attached to the release.</param>
public sealed record GitHubRelease(
    string TagName,
    string? TargetCommitish,
    DateTime? PublishedUtc,
    IReadOnlyList<GitHubReleaseAsset> Assets);
