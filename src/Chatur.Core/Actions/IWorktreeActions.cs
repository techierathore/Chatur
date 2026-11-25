namespace Chatur.Core.Actions;

/// <summary>
/// Working copies: a second folder of the same repository on its own branch, so a run's files never land
/// in the owner's folder (Architecture §7 "Source control"; page Worktrees; REQ-FN-079, REQ-FN-080, REQ-UI-068).
/// </summary>
public interface IWorktreeActions
{
    /// <summary>Every working copy of the project's repository, the owner's own folder first (REQ-FN-079).</summary>
    /// <param name="aProjectId">The project whose repository is read.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<WorkingCopy>> ListWorkingCopiesAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>The branches a new copy can take and a folder to propose for it.</summary>
    /// <param name="aProjectId">The project whose repository is read.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<WorkingCopyChoices> WorkingCopyChoicesAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Adds a working copy of a branch in a new folder (REQ-FN-079). The folder must be empty or not yet exist,
    /// and lie outside the project's own folder.
    /// </summary>
    /// <param name="aProjectId">The project whose repository gets the copy.</param>
    /// <param name="aBranch">The branch to check out in the copy.</param>
    /// <param name="aFolder">The copy's folder.</param>
    /// <param name="aNewBranch">Whether the branch is made new from the project's current commit, rather than taken as it is.</param>
    /// <param name="aCt">A token that cancels the add.</param>
    /// <exception cref="InvalidOperationException">The folder or branch cannot be used, or git refused; the message says why.</exception>
    Task<WorkingCopy> AddWorkingCopyAsync(int aProjectId, string aBranch, string aFolder, bool aNewBranch, CancellationToken aCt = default);

    /// <summary>
    /// Removes a working copy and its folder (REQ-UI-068). The owner's own folder, and a copy a run is going in, are refused.
    /// </summary>
    /// <param name="aProjectId">The project whose repository holds the copy.</param>
    /// <param name="aFolder">The copy's folder.</param>
    /// <param name="aCt">A token that cancels the removal.</param>
    /// <exception cref="InvalidOperationException">The copy is the owner's own folder, is in use, or git refused; the message says why.</exception>
    Task RemoveWorkingCopyAsync(int aProjectId, string aFolder, CancellationToken aCt = default);

    /// <summary>
    /// The project a run in a working copy is recorded against: a project row for the copy's folder that no project list
    /// shows, made on first use. A run started on it reads and writes only inside the copy (REQ-FN-080).
    /// </summary>
    /// <param name="aProjectId">The project whose repository holds the copy.</param>
    /// <param name="aFolder">The copy's folder.</param>
    /// <param name="aCt">A token that cancels the read or write.</param>
    /// <exception cref="InvalidOperationException">The folder is not a working copy of the project's repository.</exception>
    Task<int> WorkingCopyProjectAsync(int aProjectId, string aFolder, CancellationToken aCt = default);

    /// <summary>How many bytes a working copy's folder holds, for the disk figure on Working copies.</summary>
    /// <param name="aFolder">The copy's folder.</param>
    /// <param name="aCt">A token that stops the count.</param>
    Task<long> WorkingCopyBytesAsync(string aFolder, CancellationToken aCt = default);
}
