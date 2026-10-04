namespace Chatur.Core.Actions;

/// <summary>
/// Showing changes, checking in, pushing, pulling and switching branches (Architecture §7 "Source
/// control"; page Repository). Pushing and merging are always the owner's own action — a model
/// asking for a source-control command is refused by the guards (Coding Standards "A model never
/// runs a source-control command").
/// </summary>
public interface ISourceControlActions
{
    /// <summary>Every file that differs from the last check-in (REQ-UI-042).</summary>
    /// <param name="aProjectId">The project to read changes for.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ChangedFile>> ChangesAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// The old and new text of one changed file (REQ-UI-042).
    /// </summary>
    /// <param name="aProjectId">The project the file belongs to.</param>
    /// <param name="aFilePath">The file's path relative to the repository root.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<string> DiffAsync(int aProjectId, string aFilePath, CancellationToken aCt = default);

    /// <summary>
    /// Commits the chosen files with the owner's message (REQ-UI-043).
    /// </summary>
    /// <param name="aProjectId">The project to check in.</param>
    /// <param name="aFilePaths">The files to include.</param>
    /// <param name="aMessage">The check-in message.</param>
    /// <param name="aCt">A token that cancels the check-in.</param>
    Task CheckInAsync(int aProjectId, IReadOnlyList<string> aFilePaths, string aMessage, CancellationToken aCt = default);

    /// <summary>
    /// The current branch and how far ahead of or behind its upstream it is, for the pills at the
    /// top of Repository (REQ-UI-044).
    /// </summary>
    /// <param name="aProjectId">The project to read.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<SourceControlStatus> StatusAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Pushes the current branch's check-ins and returns how far ahead or behind the branch now is
    /// (REQ-UI-044). Only the owner can call this — a model's request never reaches it.
    /// </summary>
    /// <param name="aProjectId">The project to push.</param>
    /// <param name="aCt">A token that cancels the push.</param>
    Task<SourceControlStatus> PushAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Pulls what is new on the server and returns how far ahead or behind the branch now is
    /// (REQ-UI-044).
    /// </summary>
    /// <param name="aProjectId">The project to pull.</param>
    /// <param name="aCt">A token that cancels the pull.</param>
    Task<SourceControlStatus> PullAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>The branches this repository has.</summary>
    /// <param name="aProjectId">The project to list branches for.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<string>> BranchesAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Switches to another branch (REQ-FN-047).
    /// </summary>
    /// <param name="aProjectId">The project to switch.</param>
    /// <param name="aBranchName">The branch to switch to.</param>
    /// <param name="aCt">A token that cancels the switch.</param>
    Task SwitchBranchAsync(int aProjectId, string aBranchName, CancellationToken aCt = default);

    /// <summary>
    /// The newest check-ins on the current branch, newest first (BRD §4 Repository "history").
    /// </summary>
    /// <param name="aProjectId">The project to read the history of.</param>
    /// <param name="aCount">The most check-ins to return.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The check-ins, or an empty list when the repository has none yet.</returns>
    Task<IReadOnlyList<CommitEntry>> HistoryAsync(int aProjectId, int aCount, CancellationToken aCt = default);

    /// <summary>
    /// The branches a run checked in to by itself — every branch named <c>run/…</c> — with the
    /// requirement ids they carry (UI Design "Screen: Repository", <c>agent-checkins</c>; REQ-FN-048).
    /// </summary>
    /// <param name="aProjectId">The project to read the branches of.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ProcessBranch>> ProcessBranchesAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Commits a run's own automatic check-in to its own branch, with the requirement ids in the
    /// message (REQ-FN-048).
    /// </summary>
    /// <param name="aProjectId">The project the run worked in.</param>
    /// <param name="aBranchName">The run's own branch.</param>
    /// <param name="aFilePaths">The files the run changed.</param>
    /// <param name="aMessage">The check-in message, carrying the requirement ids.</param>
    /// <param name="aCt">A token that cancels the check-in.</param>
    Task RunBranchCheckInAsync(int aProjectId, string aBranchName, IReadOnlyList<string> aFilePaths, string aMessage, CancellationToken aCt = default);
}
