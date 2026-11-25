namespace Chatur.Core.Actions;

/// <summary>
/// What Chatur offers to outside tools (REQ-FN-081, REQ-FN-082), what the verifier drove on real windows
/// (REQ-FN-083) and the signed downloads of a release (REQ-UI-069).
/// </summary>
public interface IIntegrationActions
{
    /// <summary>Every action Chatur offers, with what starts it, its address and whether it is on.</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<PublishedAction>> ListActionsAsync(CancellationToken aCt = default);

    /// <summary>Turns an action on or off. An action Chatur does not yet answer cannot be turned on.</summary>
    /// <param name="aCode">The action's code.</param>
    /// <param name="aEnabled">The new state.</param>
    /// <param name="aCt">A token that cancels the save.</param>
    /// <exception cref="ArgumentException">The code is not an action, or the action is not built yet and cannot be on.</exception>
    Task SetActionEnabledAsync(string aCode, bool aEnabled, CancellationToken aCt = default);

    /// <summary>Makes a token for another tool. Only a hash is kept; the value comes back this once.</summary>
    /// <param name="aCt">A token that cancels the save.</param>
    Task<NewIntegrationToken> CreateTokenAsync(CancellationToken aCt = default);

    /// <summary>
    /// Answers an outside call to the "Start a process" address: checks the token, starts the matching
    /// process on the named project (or the open one when none is named) and records the run.
    /// </summary>
    /// <param name="aToken">The bearer token the tool sent.</param>
    /// <param name="aProject">The project's name; <see langword="null"/> for the project open in Chatur.</param>
    /// <param name="aProcess">The process to start, e.g. <c>"verify-phase"</c>.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<OutsideCallResult> StartFromOutsideAsync(string? aToken, string? aProject, string? aProcess, CancellationToken aCt = default);

    /// <summary>The runs outside tools started, newest first.</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<OutsideRun>> ListOutsideRunsAsync(CancellationToken aCt = default);

    /// <summary>The checks of built applications the verifier drove in their own windows, newest first.</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<WindowCheck>> ListWindowChecksAsync(CancellationToken aCt = default);

    /// <summary>Records one such check, with how many screenshots were kept.</summary>
    /// <param name="aCheck">The check.</param>
    /// <param name="aCt">A token that cancels the save.</param>
    /// <exception cref="ArgumentException">The platform is neither Mac Catalyst nor Windows, or no screenshot was kept.</exception>
    Task RecordWindowCheckAsync(WindowCheck aCheck, CancellationToken aCt = default);

    /// <summary>The signed downloads of the newest published release, one per platform.</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<SignedDownload>> ListDownloadsAsync(CancellationToken aCt = default);

    /// <summary>Records one signed download of a published release.</summary>
    /// <param name="aDownload">The download.</param>
    /// <param name="aCt">A token that cancels the save.</param>
    /// <exception cref="ArgumentException">Nobody is named as having signed it, or the platform is not Mac or Windows.</exception>
    Task RecordDownloadAsync(SignedDownload aDownload, CancellationToken aCt = default);
}
