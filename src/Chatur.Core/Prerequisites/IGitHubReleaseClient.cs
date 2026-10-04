namespace Chatur.Core.Prerequisites;

/// <summary>Reads the newest release published for the Chatur repository on GitHub (REQ-FN-013).</summary>
public interface IGitHubReleaseClient
{
    /// <summary>
    /// The most recently published release, or <see langword="null"/> when the repository has no
    /// releases yet, or does not exist — reported honestly rather than guessed at.
    /// </summary>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<GitHubRelease?> LatestReleaseAsync(CancellationToken aCt = default);
}
